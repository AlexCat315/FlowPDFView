package com.blaze.pdfviewer.source

import android.content.Context
import com.blaze.pdfium.PdfDocument
import com.blaze.pdfium.PdfiumCore
import java.io.IOException

class ByteArraySource(private val data: ByteArray) : DocumentSource {

    @Throws(IOException::class)
    override fun createDocument(context: Context, pdfiumCore: PdfiumCore, password: String?): PdfDocument {
        return pdfiumCore.newDocument(data = data, password = password)
    }
}