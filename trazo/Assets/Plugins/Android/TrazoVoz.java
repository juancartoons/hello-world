package com.trazovr;

import org.vosk.LibVosk;
import org.vosk.LogLevel;
import org.vosk.Model;
import org.vosk.Recognizer;

import android.media.AudioFormat;
import android.media.AudioRecord;
import android.media.MediaRecorder;

import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

// LA VOZ de JCartoons (v40), con Vosk (gratis, de código abierto y SIN internet al usarla): el visor mismo
// entiende tus palabras. Solo la primera vez baja el "modelo" del idioma (unos 40 MB) desde alphacephei.com.
// Unity (Voz.cs) le pregunta cada cuadro: estado(), progreso() y siguiente() (lo último que entendió).
// Todo lo lento (bajar, descomprimir, cargar) va en otro hilo: la app no se congela.
// v46: ya no usa SpeechService: tiene su propia "oreja" (un hilo que lee el micrófono y se lo pasa a Vosk). Así además
// GUARDA lo que oye: cada frase que dices (de cuando empiezas a hablar hasta que te callas) queda lista para que la
// app la repita con TU voz (la cara del tutorial que dice "hola, ¿cómo estás?"). Unity la pide con fraseAudio().
public class TrazoVoz {
    static final float MUESTREO = 16000f;

    static volatile String estado = "apagada";   // apagada, bajando, descomprimiendo, cargando, lista, escuchando, error: ...
    static volatile int progreso = 0;            // 0 a 100 (al bajar)
    static final ConcurrentLinkedQueue<String> oido = new ConcurrentLinkedQueue<String>();
    static Model modelo;
    static Recognizer reconocedor;
    // La oreja (v46).
    static Thread hilo;
    static volatile boolean oyendo;
    static final int ANILLO = 16000 * 10;               // los últimos 10 s de lo que oye
    static final short[] anillo = new short[ANILLO];
    static long escritos = 0;                           // muestras guardadas desde que empezó a escuchar
    static long inicioVoz = -1;                         // dónde empezó la frase que estás diciendo (-1 = callado)
    static long ultimaVoz = -1;                         // la última muestra con voz
    static float piso = 300f;                           // el ruido de fondo (se va ajustando solo)
    static volatile float[] frase = new float[0];       // la última frase completa (de -1 a 1)
    static volatile int numeroFrase = 0;                // sube cada vez que termina una frase
    static long finFrase = -1;                          // dónde terminó la última frase
    static String carpetaCargada = "";
    static String gramaticaCargada = "";

    public static String estado() {
        return estado;
    }

    public static int progreso() {
        return progreso;
    }

    // Lo siguiente que entendió (o "" si no hay nada nuevo).
    public static String siguiente() {
        String s = oido.poll();
        return s != null ? s : "";
    }

    // ¿Ya está el modelo en esta carpeta? (lo deja una marca "listo.txt" al terminar de descomprimir)
    public static boolean tieneModelo(String carpeta) {
        return new File(carpeta, "listo.txt").exists();
    }

    // Prepara todo en otro hilo: baja y descomprime el modelo si falta, lo carga y arma el reconocedor solo con
    // las frases de los comandos (así es rápido y casi no se equivoca). Al final queda "lista".
    public static void preparar(final String url, final String carpeta, final String gramatica) {
        if (estado.startsWith("bajando") || estado.startsWith("descomprimiendo") || estado.startsWith("cargando"))
            return;
        new Thread(new Runnable() {
            public void run() {
                try {
                    if (!tieneModelo(carpeta)) {
                        estado = "bajando";
                        File zip = new File(carpeta + ".zip");
                        bajar(url, zip);
                        estado = "descomprimiendo";
                        descomprimir(zip, new File(carpeta));
                        zip.delete();
                        new FileOutputStream(new File(carpeta, "listo.txt")).close();
                    }
                    estado = "cargando";
                    if (modelo == null || !carpetaCargada.equals(carpeta)) {
                        cerrarTodo();
                        LibVosk.setLogLevel(LogLevel.WARNINGS);
                        modelo = new Model(rutaDelModelo(new File(carpeta)).getAbsolutePath());
                        carpetaCargada = carpeta;
                        gramaticaCargada = "";
                    }
                    if (reconocedor == null || !gramaticaCargada.equals(gramatica)) {
                        parar();   // v46: la oreja usa el reconocedor: primero se para
                        if (reconocedor != null)
                            reconocedor.close();
                        reconocedor = new Recognizer(modelo, MUESTREO, gramatica);
                        gramaticaCargada = gramatica;
                    }
                    estado = "lista";
                } catch (Throwable e) {
                    estado = "error: " + e.getMessage();
                }
            }
        }).start();
    }

    // Empieza a escuchar (el micrófono ya tiene que tener permiso). Devuelve false si no se pudo.
    public static boolean escuchar() {
        if (reconocedor == null)
            return false;
        if (oyendo && hilo != null && hilo.isAlive()) {
            estado = "escuchando";
            return true;
        }
        try {
            int minimo = AudioRecord.getMinBufferSize((int) MUESTREO, AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT);
            final AudioRecord grabadora = new AudioRecord(MediaRecorder.AudioSource.VOICE_RECOGNITION, (int) MUESTREO,
                    AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT, Math.max(minimo, 8192) * 2);
            if (grabadora.getState() != AudioRecord.STATE_INITIALIZED) {
                grabadora.release();
                estado = "error: no se pudo abrir el micrófono";
                return false;
            }
            oido.clear();
            reconocedor.reset();
            synchronized (anillo) {
                escritos = 0;
                finFrase = -1;
                inicioVoz = -1;
                ultimaVoz = -1;
            }
            oyendo = true;
            grabadora.startRecording();
            hilo = new Thread(new Runnable() {
                public void run() {
                    oir(grabadora);
                }
            });
            hilo.start();
            estado = "escuchando";
            return true;
        } catch (Throwable e) {
            oyendo = false;
            estado = "error: " + e.getMessage();
            return false;
        }
    }

    // El hilo de la oreja: lee el micrófono de a 0.1 s, guarda, mide si hay voz y se lo pasa a Vosk.
    static void oir(AudioRecord grabadora) {
        short[] buffer = new short[1600];
        int errores = 0;
        try {
            while (oyendo) {
                int n = grabadora.read(buffer, 0, buffer.length);
                if (n <= 0) {
                    if (++errores > 50)
                        break;
                    Thread.sleep(10);
                    continue;
                }
                errores = 0;
                guardarAudio(buffer, n);
                Recognizer r = reconocedor;
                if (r == null || !oyendo)
                    break;
                if (r.acceptWaveForm(buffer, n))
                    guardar(r.getResult());
            }
        } catch (Throwable e) {
            if (oyendo)
                estado = "error: " + e.getMessage();
        }
        try {
            grabadora.stop();
        } catch (Throwable e) {
            // nada
        }
        grabadora.release();
    }

    // Guarda el pedacito en el anillo y decide si estás hablando (por el volumen comparado con el ruido de fondo).
    // Cuando te callas 0.8 s, la frase se recorta y queda lista en "frase".
    static void guardarAudio(short[] buffer, int n) {
        double suma = 0;
        for (int i = 0; i < n; i++)
            suma += (double) buffer[i] * buffer[i];
        float rms = (float) Math.sqrt(suma / Math.max(1, n));
        boolean voz = rms > Math.max(220f, piso * 2.8f);
        if (!voz)
            piso = rms < piso ? piso * 0.8f + rms * 0.2f : piso * 0.995f + rms * 0.005f;
        synchronized (anillo) {
            for (int i = 0; i < n; i++)
                anillo[(int) ((escritos + i) % ANILLO)] = buffer[i];
            long antes = escritos;
            escritos += n;
            if (voz) {
                if (inicioVoz < 0)
                    inicioVoz = Math.max(0, antes - (long) (MUESTREO * 0.3f));
                ultimaVoz = escritos;
            }
            if (inicioVoz >= 0) {
                boolean callado = escritos - ultimaVoz > (long) (MUESTREO * 0.8f);
                boolean largo = escritos - inicioVoz > ANILLO - (long) MUESTREO;
                if (callado || largo) {
                    long desde = Math.max(inicioVoz, escritos - ANILLO + 1);
                    long hasta = Math.min(escritos, ultimaVoz + (long) (MUESTREO * 0.25f));
                    int largoFrase = (int) Math.max(0, hasta - desde);
                    // Menos de 0.25 s no es una frase (un golpe, un ruido).
                    if (ultimaVoz - inicioVoz > (long) (MUESTREO * 0.25f) && largoFrase > 0) {
                        float[] f = new float[largoFrase];
                        for (int i = 0; i < largoFrase; i++)
                            f[i] = anillo[(int) ((desde + i) % ANILLO)] / 32768f;
                        frase = f;
                        finFrase = hasta;
                        numeroFrase++;
                    }
                    inicioVoz = -1;
                }
            }
        }
    }

    // v46: la última frase que dijiste (muestras de -1 a 1, a 16000 por segundo). Vacía si no hay.
    public static float[] fraseAudio() {
        float[] f = frase;
        return f != null ? f : new float[0];
    }

    // v46: cuántas frases terminaron (para saber si ya llegó una nueva) y si estás hablando ahora mismo.
    public static int numeroFrase() {
        return numeroFrase;
    }

    public static boolean hablando() {
        synchronized (anillo) {
            return inicioVoz >= 0;
        }
    }

    // Segundos desde que terminó la última frase (mucho si no hay ninguna).
    public static float segundosDesdeFrase() {
        synchronized (anillo) {
            return finFrase < 0 ? 9999f : (escritos - finFrase) / MUESTREO;
        }
    }

    public static int muestreo() {
        return (int) MUESTREO;
    }

    // Deja de escuchar (el modelo se queda cargado, para volver rápido).
    public static void parar() {
        oyendo = false;
        Thread h = hilo;
        hilo = null;
        if (h != null) {
            try {
                h.join(1500);
            } catch (Throwable e) {
                // nada
            }
        }
        if (estado.equals("escuchando"))
            estado = "lista";
    }

    static void cerrarTodo() {
        parar();
        if (reconocedor != null) {
            reconocedor.close();
            reconocedor = null;
        }
        if (modelo != null) {
            modelo.close();
            modelo = null;
        }
    }

    // El resultado viene así: { "text" : "gira izquierda" }. Solo se guarda el texto (y nunca "[unk]" = no entendió).
    static void guardar(String json) {
        if (json == null)
            return;
        int i = json.indexOf("\"text\"");
        if (i < 0)
            return;
        int a = json.indexOf('"', json.indexOf(':', i) + 1);
        int b = a >= 0 ? json.indexOf('"', a + 1) : -1;
        if (a < 0 || b <= a)
            return;
        String texto = json.substring(a + 1, b).replace("[unk]", "").trim();
        if (texto.length() > 0)
            oido.add(texto);
    }

    static void bajar(String direccion, File destino) throws Exception {
        destino.getParentFile().mkdirs();
        HttpURLConnection c = (HttpURLConnection) new URL(direccion).openConnection();
        c.setConnectTimeout(20000);
        c.setReadTimeout(30000);
        c.setInstanceFollowRedirects(true);
        int codigo = c.getResponseCode();
        if (codigo != 200)
            throw new Exception("no se pudo bajar el modelo (" + codigo + ")");
        long total = c.getContentLength();
        InputStream entrada = new BufferedInputStream(c.getInputStream());
        OutputStream salida = new FileOutputStream(destino);
        try {
            byte[] buffer = new byte[1 << 16];
            long llevo = 0;
            int n;
            while ((n = entrada.read(buffer)) > 0) {
                salida.write(buffer, 0, n);
                llevo += n;
                if (total > 0)
                    progreso = (int) (llevo * 100 / total);
            }
        } finally {
            salida.close();
            entrada.close();
            c.disconnect();
        }
    }

    static void descomprimir(File zip, File carpeta) throws Exception {
        carpeta.mkdirs();
        String base = carpeta.getCanonicalPath();
        ZipInputStream z = new ZipInputStream(new BufferedInputStream(new java.io.FileInputStream(zip)));
        try {
            ZipEntry e;
            byte[] buffer = new byte[1 << 16];
            while ((e = z.getNextEntry()) != null) {
                File f = new File(carpeta, e.getName());
                if (!f.getCanonicalPath().startsWith(base))
                    continue; // nada fuera de la carpeta
                if (e.isDirectory()) {
                    f.mkdirs();
                    continue;
                }
                f.getParentFile().mkdirs();
                OutputStream salida = new FileOutputStream(f);
                try {
                    int n;
                    while ((n = z.read(buffer)) > 0)
                        salida.write(buffer, 0, n);
                } finally {
                    salida.close();
                }
            }
        } finally {
            z.close();
        }
    }

    // El zip trae una carpeta adentro (vosk-model-small-es-0.42/...): el modelo es la que tiene "am" o "conf".
    static File rutaDelModelo(File carpeta) {
        if (new File(carpeta, "am").exists() || new File(carpeta, "conf").exists())
            return carpeta;
        File[] hijos = carpeta.listFiles();
        if (hijos != null)
            for (File h : hijos)
                if (h.isDirectory() && (new File(h, "am").exists() || new File(h, "conf").exists()))
                    return h;
        return carpeta;
    }
}
