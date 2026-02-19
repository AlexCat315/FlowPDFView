package com.blaze.pdfviewer.source

import android.content.Context
import com.blaze.pdfium.PdfDocument
import com.blaze.pdfium.PdfiumCore
import java.io.IOException

interface DocumentSource {

    @Throws(IOException::class)
    fun createDocument(context: Context, pdfiumCore: PdfiumCore, password: String?): PdfDocument
}