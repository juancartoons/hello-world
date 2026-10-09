package com.trazovr;

import android.content.ContentResolver;
import android.content.ContentUris;
import android.content.Context;
import android.database.Cursor;
import android.graphics.Bitmap;
import android.net.Uri;
import android.os.Build;
import android.provider.MediaStore;
import android.util.Size;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

// Las imágenes de TODO el Quest (Descargas, Cámara, capturas, WhatsApp, Facebook...) para el buscador
// de imágenes de JCartoons. Usa la galería del sistema (MediaStore): necesita el permiso de leer fotos.
public class TrazoImagenes {
    // Una línea por imagen: id \t nombre \t carpeta \t fecha (segundos) \t ancho \t alto. Las más nuevas primero.
    public static String listar(Context ctx, int maximo) {
        if (ctx == null)
            return "";
        StringBuilder sb = new StringBuilder();
        Cursor c = null;
        try {
            String[] columnas = { "_id", "_display_name", "bucket_display_name", "date_modified", "width", "height" };
            c = ctx.getContentResolver().query(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, columnas, null, null,
                    "date_modified DESC");
            if (c == null)
                return "";
            int n = 0;
            while (n < maximo && c.moveToNext()) {
                sb.append(c.getLong(0)).append('\t')
                  .append(limpiar(c.getString(1))).append('\t')
                  .append(limpiar(c.getString(2))).append('\t')
                  .append(c.getLong(3)).append('\t')
                  .append(c.getInt(4)).append('\t')
                  .append(c.getInt(5)).append('\n');
                n++;
            }
        } catch (Exception e) {
            return "ERROR\t" + limpiar(e.getMessage());
        } finally {
            if (c != null)
                c.close();
        }
        return sb.toString();
    }

    static String limpiar(String s) {
        if (s == null)
            return "";
        return s.replace('\t', ' ').replace('\n', ' ').replace('\r', ' ');
    }

    static Uri uriDe(long id) {
        return ContentUris.withAppendedId(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, id);
    }

    // Guarda una miniatura (jpg, de "tam" pixeles como máximo) en "destino". true si se pudo.
    public static boolean miniatura(Context ctx, long id, int tam, String destino) {
        if (ctx == null)
            return false;
        Bitmap b = null;
        FileOutputStream salida = null;
        try {
            ContentResolver cr = ctx.getContentResolver();
            if (Build.VERSION.SDK_INT >= 29)
                b = cr.loadThumbnail(uriDe(id), new Size(tam, tam), null);
            else
                b = MediaStore.Images.Thumbnails.getThumbnail(cr, id, MediaStore.Images.Thumbnails.MINI_KIND, null);
            if (b == null)
                return false;
            salida = new FileOutputStream(destino);
            return b.compress(Bitmap.CompressFormat.JPEG, 88, salida);
        } catch (Throwable e) {
            return false;
        } finally {
            try {
                if (salida != null)
                    salida.close();
            } catch (Exception e) {
                // nada
            }
            if (b != null)
                b.recycle();
        }
    }

    // Copia la imagen completa a "destino" (por ejemplo, la carpeta de imágenes de la app). true si se pudo.
    public static boolean copiar(Context ctx, long id, String destino) {
        if (ctx == null)
            return false;
        InputStream entrada = null;
        OutputStream salida = null;
        boolean bien = false;
        try {
            entrada = ctx.getContentResolver().openInputStream(uriDe(id));
            if (entrada == null)
                return false;
            salida = new FileOutputStream(destino);
            byte[] buffer = new byte[1 << 16];
            int n;
            while ((n = entrada.read(buffer)) > 0)
                salida.write(buffer, 0, n);
            salida.close();
            salida = null;
            bien = true;
            return true;
        } catch (Exception e) {
            return false;
        } finally {
            try {
                if (entrada != null)
                    entrada.close();
            } catch (Exception e) {
                // nada
            }
            try {
                if (salida != null)
                    salida.close();
            } catch (Exception e) {
                // nada
            }
            if (!bien)
                new File(destino).delete();
        }
    }
}
