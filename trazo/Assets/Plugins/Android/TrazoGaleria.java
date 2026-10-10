package com.trazovr;

import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Context;
import android.net.Uri;
import android.os.Build;
import android.provider.MediaStore;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.io.OutputStream;

// Copia un archivo de JCartoons (video, foto o SVG) a una carpeta pública del Quest (Download/JCartoons),
// para que se vea en la app "Archivos" del visor y se pueda pasar fácil al teléfono o al PC.
public class TrazoGaleria {
    public static String publicar(Context ctx, String ruta, String mime, String carpeta) {
        if (ctx == null || Build.VERSION.SDK_INT < 29)
            return "";
        ContentResolver cr = null;
        Uri uri = null;
        try {
            File f = new File(ruta);
            if (!f.exists())
                return "";
            cr = ctx.getContentResolver();
            ContentValues v = new ContentValues();
            v.put(MediaStore.MediaColumns.DISPLAY_NAME, f.getName());
            v.put(MediaStore.MediaColumns.MIME_TYPE, mime);
            v.put(MediaStore.MediaColumns.RELATIVE_PATH, carpeta);
            v.put(MediaStore.MediaColumns.IS_PENDING, 1);
            // Descargas acepta cualquier archivo (SVG, MP4, PNG...): ahí se encuentra todo junto.
            Uri coleccion = carpeta.startsWith("Download")
                    ? MediaStore.Downloads.EXTERNAL_CONTENT_URI
                    : mime.startsWith("video")
                    ? MediaStore.Video.Media.EXTERNAL_CONTENT_URI
                    : MediaStore.Images.Media.EXTERNAL_CONTENT_URI;
            uri = cr.insert(coleccion, v);
            if (uri == null)
                return "";
            InputStream entrada = new FileInputStream(f);
            OutputStream salida = cr.openOutputStream(uri);
            try {
                if (salida == null)
                    throw new Exception("sin salida");
                byte[] buffer = new byte[1 << 16];
                int n;
                while ((n = entrada.read(buffer)) > 0)
                    salida.write(buffer, 0, n);
            } finally {
                entrada.close();
                if (salida != null)
                    salida.close();
            }
            ContentValues listo = new ContentValues();
            listo.put(MediaStore.MediaColumns.IS_PENDING, 0);
            cr.update(uri, listo, null, null);
            return carpeta + "/" + f.getName();
        } catch (Exception e) {
            try {
                if (cr != null && uri != null)
                    cr.delete(uri, null, null);
            } catch (Exception e2) {
                // nada
            }
            return "";
        }
    }
}
