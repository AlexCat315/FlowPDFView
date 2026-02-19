package com.blaze.pdfviewer.model

import android.graphics.RectF
import com.blaze.pdfium.PdfDocument

class LinkTapEvent(
    val originalX: Float,
    val originalY: Float,
    val mappedX: Float,
    val mappedY: Float,
    val mappedLinkRect: RectF,
    val link: PdfDocument.Link
)