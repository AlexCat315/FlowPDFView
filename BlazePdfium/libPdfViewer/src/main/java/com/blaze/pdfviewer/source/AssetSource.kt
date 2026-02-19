package com.blaze.pdfviewer.source

import android.content.Context
import android.os.ParcelFileDescriptor
import com.blaze.pdfium.PdfDocument
import com.blaze.pdfium.PdfiumCore
import com.blaze.pdfviewer.util.PdfUtils
import java.io.IOException


class AssetSource(private val name: String) : DocumentSource {

    @Throws(IOException::class)
    override fun createDocument(context: Context, pdfiumCore: PdfiumCore, password: String?): PdfDocument {
        return pdfiumCore.newDocument(
            parcelFileDescriptor = ParcelFileDescriptor.open(
                PdfUtils.fileFromAsset(context = context, assetName = name),
                ParcelFileDescriptor.MODE_READ_ONLY
            ),
            password = password
        )
    }
}