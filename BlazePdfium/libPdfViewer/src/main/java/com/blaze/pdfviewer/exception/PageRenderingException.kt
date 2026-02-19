package com.blaze.pdfviewer.exception

class PageRenderingException(val page: Int, cause: Throwable?) : Exception(cause)