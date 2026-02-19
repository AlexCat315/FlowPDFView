package com.blaze.pdfviewer.source

import android.content.Context
import com.blaze.pdfium.PdfDocument
import com.blaze.pdfium.PdfiumCore
import com.blaze.pdfviewer.util.PdfUtils
import java.io.IOException
import java.io.InputStream

class InputStreamSource(private val inputStream: InputStream) : DocumentSource {

    @Throws(IOException::class)
    override fun createDocument(context: Context, pdfiumCore: PdfiumCore, password: String?): PdfDocument {
        return pdfiumCore.newDocument(data = PdfUtils.toByteArray(inputStream = inputStream), password = password)
    }
}