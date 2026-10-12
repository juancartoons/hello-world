package com.trazovr;

import org.vosk.LibVosk;
import org.vosk.LogLevel;
import org.vosk.Model;
import org.vosk.Recognizer;
import org.vosk.android.RecognitionListener;
import org.vosk.android.SpeechService;

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
public class TrazoVoz {
    static final float MUESTREO = 16000f;

    static volatile String estado = "apagada";   // apagada, bajando, descomprimiendo, cargando, lista, escuchando, error: ...
    static volatile int progreso = 0;            // 0 a 100 (al bajar)
    static final ConcurrentLinkedQueue<String> oido = new ConcurrentLinkedQueue<String>();
    static Model modelo;
    static Recognizer reconocedor;
    static SpeechService servicio;
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
        try {
            if (servicio == null)
                servicio = new SpeechService(reconocedor, MUESTREO);
            oido.clear();
            boolean ok = servicio.startListening(new Oyente());
            if (ok)
                estado = "escuchando";
            return ok;
        } catch (Throwable e) {
            estado = "error: " + e.getMessage();
            return false;
        }
    }

    // Deja de escuchar (el modelo se queda cargado, para volver rápido).
    public static void parar() {
        try {
            if (servicio != null) {
                servicio.stop();
                servicio.shutdown();
                servicio = null;
            }
        } catch (Throwable e) {
            // nada
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

    static class Oyente implements RecognitionListener {
        public void onPartialResult(String hipotesis) {
        }

        public void onResult(String hipotesis) {
            guardar(hipotesis);
        }

        public void onFinalResult(String hipotesis) {
            guardar(hipotesis);
        }

        public void onError(Exception e) {
            estado = "error: " + (e != null ? e.getMessage() : "");
        }

        public void onTimeout() {
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
