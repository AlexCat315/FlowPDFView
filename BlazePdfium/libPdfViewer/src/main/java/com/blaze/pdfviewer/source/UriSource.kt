package com.blaze.pdfviewer.source

import android.content.Context
import android.net.Uri
import android.os.ParcelFileDescriptor
import com.blaze.pdfium.PdfDocument
import com.blaze.pdfium.PdfiumCore
import java.io.IOException

class UriSource(private val uri: Uri) : DocumentSource {

    @Throws(IOException::class)
    override fun createDocument(context: Context, pdfiumCore: PdfiumCore, password: String?): PdfDocument {
        val parcelFileDescriptor: ParcelFileDescriptor = context.contentResolver.openFileDescriptor(uri, "r")
            ?: throw IOException("Failed to open PDF from URI")
        return pdfiumCore.newDocument(parcelFileDescriptor = parcelFileDescriptor, password = password)
    }
}