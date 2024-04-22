using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using UnityEngine;
using Xilium.CefGlue;

namespace StationeersWebDisplay.Cef
{
    internal class OffscreenCefClient : CefClient
    {
        private readonly DialogHandler _dialogHandler = new();
        private readonly DownloadHandler _downloadHandler = new();
        private readonly LifeSpanHandler _lifespanHandler = new();
        private readonly RenderHandler _renderHandler;

        private readonly object PixelLock = new object();
        private byte[] _pixelBuffer;

        public OffscreenCefClient(Size windowSize)
        {
            this._pixelBuffer = new byte[windowSize.Width * windowSize.Height * 4];
            this._renderHandler = new(windowSize.Width, windowSize.Height, this);
        }

        public void CopyToTexture(Texture2D pTexture)
        {
            lock (this.PixelLock)
            {
                pTexture.LoadRawTextureData(this._pixelBuffer);
                pTexture.Apply(false);
            }
        }

        protected override CefDialogHandler GetDialogHandler()
        {
            return this._dialogHandler;
        }

        protected override CefDownloadHandler GetDownloadHandler()
        {
            return this._downloadHandler;
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return this._lifespanHandler;
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return this._renderHandler;
        }

        // TODO: Request handler, only allow requests to whitelisted sites / the api.

        private class DialogHandler : CefDialogHandler
        {
            protected override bool OnFileDialog(CefBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, string[] acceptFilters, int selectedAcceptFilter, CefFileDialogCallback callback)
            {
                return false;
            }
        }

        private class DownloadHandler : CefDownloadHandler
        {
            protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
            {
                // TODO: How do we cancel, just ignore?
            }
        }

        private class LifeSpanHandler : CefLifeSpanHandler
        {
            protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
            {
                return false;
            }
        }

        internal class RenderHandler : CefRenderHandler
        {
            private readonly AccessibilityHandler _accessibilityHandler = new();

            private readonly OffscreenCefClient client;

            private readonly int _windowWidth;
            private readonly int _windowHeight;

            public RenderHandler(int windowWidth, int windowHeight, OffscreenCefClient client)
            {
                this._windowWidth = windowWidth;
                this._windowHeight = windowHeight;
                this.client = client;
            }

            protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
            {
                rect.X = 0;
                rect.Y = 0;
                rect.Width = this._windowWidth;
                rect.Height = this._windowHeight;
                return true;
            }

            protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
            {
                rect = new();
                rect.X = 0;
                rect.Y = 0;
                rect.Width = this._windowWidth;
                rect.Height = this._windowHeight;
            }

            protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY)
            {
                screenX = viewX;
                screenY = viewY;
                return true;
            }

            [SecurityCritical]
            protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
            {
                if (browser != null)
                {
                    lock (client.PixelLock)
                    {
                        if (browser != null)
                            Marshal.Copy(buffer, this.client._pixelBuffer, 0, this.client._pixelBuffer.Length);
                    }
                }
            }

            protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle)
            {
            }

            protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
            {
                return false;
            }

            protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
            {
            }

            protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
            {
            }

            protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
            {
            }

            protected override CefAccessibilityHandler GetAccessibilityHandler()
            {
                return this._accessibilityHandler;
            }
        }

        private class AccessibilityHandler : CefAccessibilityHandler
        {
            protected override void OnAccessibilityLocationChange(CefValue value)
            {
            }

            protected override void OnAccessibilityTreeChange(CefValue value)
            {
            }
        }
    }
}
