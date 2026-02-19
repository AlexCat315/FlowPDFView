package com.blaze.pdfviewer.link

import com.blaze.pdfviewer.model.LinkTapEvent

interface LinkHandler {
    /**
     * Called when link was tapped by user
     *
     * @param event current event
     */
    fun handleLinkEvent(event: LinkTapEvent?)
}