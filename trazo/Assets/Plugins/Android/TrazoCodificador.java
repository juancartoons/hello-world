package com.trazovr;

import android.media.Image;
import android.media.MediaCodec;
import android.media.MediaCodecInfo;
import android.media.MediaFormat;
import android.media.MediaMuxer;

import java.nio.ByteBuffer;
import java.util.ArrayList;

// Codificador de video MP4 (H.264) con audio opcional (AAC) para TrazoVR.
// Lo usa CodificadorVideo.cs desde Unity: iniciar -> (agregarAudio) -> agregarCuadro... -> terminar.
public class TrazoCodificador {
    private static final String TIPO_VIDEO = "video/avc";
    private static final String TIPO_AUDIO = "audio/mp4a-latm";
    private static final long ESPERA = 10000; // microsegundos

    private MediaCodec video;
    private MediaMuxer muxer;
    private final MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();
    private int pistaVideo = -1;
    private int pistaAudio = -1;
    private boolean muxerIniciado = false;
    private int ancho;
    private int alto;
    private int fps;
    private long cuadros = 0;
    private byte[] nv12;
    private String error = "";

    private MediaFormat formatoAudio = null;
    private final ArrayList<byte[]> muestrasAudio = new ArrayList<byte[]>();
    private final ArrayList<long[]> tiemposAudio = new ArrayList<long[]>();

    public String obtenerError() {
        return error;
    }

    public boolean iniciar(String ruta, int ancho, int alto, int fps, int bitrate) {
        try {
            this.ancho = ancho;
            this.alto = alto;
            this.fps = fps;
            MediaFormat formato = MediaFormat.createVideoFormat(TIPO_VIDEO, ancho, alto);
            formato.setInteger(MediaFormat.KEY_COLOR_FORMAT, MediaCodecInfo.CodecCapabilities.COLOR_FormatYUV420Flexible);
            formato.setInteger(MediaFormat.KEY_BIT_RATE, bitrate);
            formato.setInteger(MediaFormat.KEY_FRAME_RATE, fps);
            formato.setInteger(MediaFormat.KEY_I_FRAME_INTERVAL, 1);
            video = MediaCodec.createEncoderByType(TIPO_VIDEO);
            video.configure(formato, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE);
            video.start();
            muxer = new MediaMuxer(ruta, MediaMuxer.OutputFormat.MUXER_OUTPUT_MPEG_4);
            nv12 = new byte[ancho * alto * 3 / 2];
            return true;
        } catch (Exception e) {
            error = "iniciar: " + e;
            liberar();
            return false;
        }
    }

    // Codifica todo el audio de una vez (PCM 16 bits) y lo guarda hasta que el video empiece.
    public boolean agregarAudio(byte[] pcm, int frecuencia, int canales) {
        MediaCodec audio = null;
        try {
            MediaFormat formato = MediaFormat.createAudioFormat(TIPO_AUDIO, frecuencia, canales);
            formato.setInteger(MediaFormat.KEY_AAC_PROFILE, MediaCodecInfo.CodecProfileLevel.AACObjectLC);
            formato.setInteger(MediaFormat.KEY_BIT_RATE, 128000);
            formato.setInteger(MediaFormat.KEY_MAX_INPUT_SIZE, 16384);
            audio = MediaCodec.createEncoderByType(TIPO_AUDIO);
            audio.configure(formato, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE);
            audio.start();

            MediaCodec.BufferInfo bi = new MediaCodec.BufferInfo();
            int bytesPorMuestra = 2 * canales;
            int pos = 0;
            boolean entradaTerminada = false;
            boolean salidaTerminada = false;
            int vacios = 0;
            while (!salidaTerminada) {
                if (!entradaTerminada) {
                    int ie = audio.dequeueInputBuffer(ESPERA);
                    if (ie >= 0) {
                        ByteBuffer b = audio.getInputBuffer(ie);
                        b.clear();
                        int n = Math.min(b.remaining(), pcm.length - pos);
                        n -= n % bytesPorMuestra;
                        long pts = (long) (pos / bytesPorMuestra) * 1000000L / frecuencia;
                        if (n <= 0) {
                            audio.queueInputBuffer(ie, 0, 0, pts, MediaCodec.BUFFER_FLAG_END_OF_STREAM);
                            entradaTerminada = true;
                        } else {
                            b.put(pcm, pos, n);
                            audio.queueInputBuffer(ie, 0, n, pts, 0);
                            pos += n;
                        }
                    }
                }
                int io = audio.dequeueOutputBuffer(bi, ESPERA);
                if (io == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {
                    formatoAudio = audio.getOutputFormat();
                } else if (io >= 0) {
                    vacios = 0;
                    ByteBuffer o = audio.getOutputBuffer(io);
                    if ((bi.flags & MediaCodec.BUFFER_FLAG_CODEC_CONFIG) == 0 && bi.size > 0 && o != null) {
                        byte[] copia = new byte[bi.size];
                        o.position(bi.offset);
                        o.get(copia, 0, bi.size);
                        muestrasAudio.add(copia);
                        tiemposAudio.add(new long[] { bi.presentationTimeUs, bi.flags });
                    }
                    audio.releaseOutputBuffer(io, false);
                    if ((bi.flags & MediaCodec.BUFFER_FLAG_END_OF_STREAM) != 0)
                        salidaTerminada = true;
                } else if (io == MediaCodec.INFO_TRY_AGAIN_LATER) {
                    vacios++;
                    if (vacios > 3000)
                        break;
                }
            }
            return formatoAudio != null;
        } catch (Exception e) {
            error = "audio: " + e;
            formatoAudio = null;
            muestrasAudio.clear();
            tiemposAudio.clear();
            return false;
        } finally {
            if (audio != null) {
                try { audio.stop(); } catch (Exception e) { }
                try { audio.release(); } catch (Exception e) { }
            }
        }
    }

    // rgba: ancho * alto * 4 bytes, filas de abajo hacia arriba (como las entrega Unity).
    public boolean agregarCuadro(byte[] rgba) {
        try {
            int indice = -1;
            for (int intento = 0; intento < 200 && indice < 0; intento++) {
                indice = video.dequeueInputBuffer(ESPERA);
                if (indice < 0)
                    drenarVideo(false);
            }
            if (indice < 0) {
                error = "el codificador no recibe cuadros";
                return false;
            }
            Image imagen = video.getInputImage(indice);
            if (imagen != null) {
                llenarImagen(imagen, rgba);
            } else {
                llenarNv12(rgba);
                ByteBuffer b = video.getInputBuffer(indice);
                b.clear();
                b.put(nv12);
            }
            long pts = cuadros * 1000000L / fps;
            video.queueInputBuffer(indice, 0, ancho * alto * 3 / 2, pts, 0);
            cuadros++;
            drenarVideo(false);
            return true;
        } catch (Exception e) {
            error = "cuadro: " + e;
            return false;
        }
    }

    // Cuadro ya convertido a NV12 en la tarjeta gráfica (Y y luego UV intercalados, filas de arriba hacia abajo).
    // Es mucho más rápido: aquí solo se copia, sin convertir píxel por píxel.
    public boolean agregarCuadroNv12(byte[] datos) {
        try {
            int indice = -1;
            for (int intento = 0; intento < 200 && indice < 0; intento++) {
                indice = video.dequeueInputBuffer(ESPERA);
                if (indice < 0)
                    drenarVideo(false);
            }
            if (indice < 0) {
                error = "el codificador no recibe cuadros";
                return false;
            }
            Image imagen = video.getInputImage(indice);
            if (imagen != null) {
                llenarImagenNv12(imagen, datos);
            } else {
                ByteBuffer b = video.getInputBuffer(indice);
                b.clear();
                b.put(datos, 0, Math.min(datos.length, b.remaining()));
            }
            long pts = cuadros * 1000000L / fps;
            video.queueInputBuffer(indice, 0, ancho * alto * 3 / 2, pts, 0);
            cuadros++;
            drenarVideo(false);
            return true;
        } catch (Exception e) {
            error = "cuadro nv12: " + e;
            return false;
        }
    }

    private void llenarImagenNv12(Image imagen, byte[] datos) {
        Image.Plane[] planos = imagen.getPlanes();
        ByteBuffer pY = planos[0].getBuffer();
        int filaY = planos[0].getRowStride();
        int pasoY = planos[0].getPixelStride();
        if (pasoY == 1) {
            for (int j = 0; j < alto; j++) {
                pY.position(j * filaY);
                pY.put(datos, j * ancho, ancho);
            }
        } else {
            for (int j = 0; j < alto; j++)
                for (int i = 0; i < ancho; i++)
                    pY.put(j * filaY + i * pasoY, datos[j * ancho + i]);
        }
        ByteBuffer pU = planos[1].getBuffer();
        ByteBuffer pV = planos[2].getBuffer();
        int filaU = planos[1].getRowStride();
        int pasoU = planos[1].getPixelStride();
        int filaV = planos[2].getRowStride();
        int pasoV = planos[2].getPixelStride();
        int tamY = ancho * alto;
        for (int j = 0; j < alto / 2; j++) {
            int base = tamY + j * ancho;
            for (int i = 0; i < ancho / 2; i++) {
                pU.put(j * filaU + i * pasoU, datos[base + 2 * i]);
                pV.put(j * filaV + i * pasoV, datos[base + 2 * i + 1]);
            }
        }
    }

    public boolean terminar() {
        try {
            int indice = -1;
            for (int intento = 0; intento < 200 && indice < 0; intento++) {
                indice = video.dequeueInputBuffer(ESPERA);
                if (indice < 0)
                    drenarVideo(false);
            }
            if (indice >= 0)
                video.queueInputBuffer(indice, 0, 0, cuadros * 1000000L / fps, MediaCodec.BUFFER_FLAG_END_OF_STREAM);
            drenarVideo(true);
            return muxerIniciado;
        } catch (Exception e) {
            error = "terminar: " + e;
            return false;
        } finally {
            liberar();
        }
    }

    private void drenarVideo(boolean hastaElFinal) {
        int vacios = 0;
        while (true) {
            int indice = video.dequeueOutputBuffer(info, hastaElFinal ? ESPERA : 0);
            if (indice == MediaCodec.INFO_TRY_AGAIN_LATER) {
                if (!hastaElFinal)
                    return;
                vacios++;
                if (vacios > 500)
                    return;
            } else if (indice == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {
                if (!muxerIniciado) {
                    pistaVideo = muxer.addTrack(video.getOutputFormat());
                    if (formatoAudio != null)
                        pistaAudio = muxer.addTrack(formatoAudio);
                    muxer.start();
                    muxerIniciado = true;
                    escribirAudioGuardado();
                }
            } else if (indice >= 0) {
                ByteBuffer datos = video.getOutputBuffer(indice);
                if ((info.flags & MediaCodec.BUFFER_FLAG_CODEC_CONFIG) != 0)
                    info.size = 0;
                if (info.size > 0 && muxerIniciado && datos != null) {
                    datos.position(info.offset);
                    datos.limit(info.offset + info.size);
                    muxer.writeSampleData(pistaVideo, datos, info);
                }
                video.releaseOutputBuffer(indice, false);
                if ((info.flags & MediaCodec.BUFFER_FLAG_END_OF_STREAM) != 0)
                    return;
            }
        }
    }

    private void escribirAudioGuardado() {
        if (pistaAudio < 0)
            return;
        MediaCodec.BufferInfo bi = new MediaCodec.BufferInfo();
        for (int k = 0; k < muestrasAudio.size(); k++) {
            byte[] d = muestrasAudio.get(k);
            long[] t = tiemposAudio.get(k);
            int flags = ((int) t[1]) & ~MediaCodec.BUFFER_FLAG_END_OF_STREAM;
            bi.set(0, d.length, t[0], flags);
            muxer.writeSampleData(pistaAudio, ByteBuffer.wrap(d), bi);
        }
        muestrasAudio.clear();
        tiemposAudio.clear();
    }

    private static int limitar(int v) {
        return v < 0 ? 0 : (v > 255 ? 255 : v);
    }

    // Convierte RGBA a YUV 4:2:0 escribiendo en los planos de la imagen del codificador.
    private void llenarImagen(Image imagen, byte[] rgba) {
        Image.Plane[] planos = imagen.getPlanes();
        ByteBuffer pY = planos[0].getBuffer();
        ByteBuffer pU = planos[1].getBuffer();
        ByteBuffer pV = planos[2].getBuffer();
        int filaY = planos[0].getRowStride();
        int pasoY = planos[0].getPixelStride();
        int filaU = planos[1].getRowStride();
        int pasoU = planos[1].getPixelStride();
        int filaV = planos[2].getRowStride();
        int pasoV = planos[2].getPixelStride();
        for (int j = 0; j < alto; j++) {
            int origen = (alto - 1 - j) * ancho * 4;
            for (int i = 0; i < ancho; i++) {
                int p = origen + i * 4;
                int r = rgba[p] & 0xff;
                int g = rgba[p + 1] & 0xff;
                int b = rgba[p + 2] & 0xff;
                int y = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
                pY.put(j * filaY + i * pasoY, (byte) limitar(y));
                if ((j & 1) == 0 && (i & 1) == 0) {
                    int u = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
                    int v = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;
                    pU.put((j / 2) * filaU + (i / 2) * pasoU, (byte) limitar(u));
                    pV.put((j / 2) * filaV + (i / 2) * pasoV, (byte) limitar(v));
                }
            }
        }
    }

    // Plan B: NV12 (Y y luego UV intercalados) en un arreglo.
    private void llenarNv12(byte[] rgba) {
        int tamY = ancho * alto;
        for (int j = 0; j < alto; j++) {
            int origen = (alto - 1 - j) * ancho * 4;
            for (int i = 0; i < ancho; i++) {
                int p = origen + i * 4;
                int r = rgba[p] & 0xff;
                int g = rgba[p + 1] & 0xff;
                int b = rgba[p + 2] & 0xff;
                int y = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
                nv12[j * ancho + i] = (byte) limitar(y);
                if ((j & 1) == 0 && (i & 1) == 0) {
                    int u = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
                    int v = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;
                    int k = tamY + (j / 2) * ancho + i;
                    nv12[k] = (byte) limitar(u);
                    nv12[k + 1] = (byte) limitar(v);
                }
            }
        }
    }

    private void liberar() {
        if (video != null) {
            try { video.stop(); } catch (Exception e) { }
            try { video.release(); } catch (Exception e) { }
            video = null;
        }
        if (muxer != null) {
            if (muxerIniciado) {
                try { muxer.stop(); } catch (Exception e) { }
            }
            try { muxer.release(); } catch (Exception e) { }
            muxer = null;
        }
        muxerIniciado = false;
    }
}
