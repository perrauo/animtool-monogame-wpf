﻿// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameOnWpfCore.MonoGame
{
    public sealed class MonoGameDrawingSurface : ContentControl, IDisposable
    {
        public MonoGameDrawingSurface()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private MonoGameGraphicsDeviceService _graphicsDeviceService;
        private D3DImage _d3DImage;
        private RenderTarget2D _renderTarget;
        private SharpDX.Direct3D9.Texture _renderTargetD3D9;
        private bool _contentNeedsRefresh = true;
        private bool _isFirstLoad = true;
        private bool _isInitialized;

        public event EventHandler<GraphicsDeviceEventArgs> LoadContent;
        public event EventHandler<DrawEventArgs> Draw;
        public bool AlwaysRefresh { get; set; }
        public GraphicsDevice GraphicsDevice => _graphicsDeviceService?.GraphicsDevice;

        private void Initialize()
        {
            _graphicsDeviceService = MonoGameGraphicsDeviceService.Singleton;

            if (_graphicsDeviceService == null)
                throw new InvalidOperationException($"{nameof(MonoGameGraphicsDeviceService)} must be initialized before {nameof(MonoGameDrawingSurface)}");

            _d3DImage = new D3DImage();

            var image = new Image { Source = _d3DImage, Stretch = Stretch.None };
            AddChild(image);

            _d3DImage.IsFrontBufferAvailableChanged += OnD3DImageIsFrontBufferAvailableChanged;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (!_isInitialized)
                return;

            RemoveBackBufferReference();
            _contentNeedsRefresh = true;

            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                Initialize();
                _isInitialized = true;
            }

            _renderTarget = CreateRenderTarget();
            CompositionTarget.Rendering += OnCompositionTargetRendering;
            _contentNeedsRefresh = true;

            if (_isFirstLoad)
            {
                var args = new GraphicsDeviceEventArgs(_graphicsDeviceService);
                LoadContent?.Invoke(this, args);
                _isFirstLoad = false;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_graphicsDeviceService != null)
            {
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
                RemoveBackBufferReference();
                _graphicsDeviceService.DeviceResetting -= OnGraphicsDeviceServiceDeviceResetting;
            }
        }

        private void OnGraphicsDeviceServiceDeviceResetting(object sender, EventArgs e)
        {
            RemoveBackBufferReference();
            _contentNeedsRefresh = true;
        }

        private void RemoveBackBufferReference()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            if (_renderTargetD3D9 != null)
            {
                _renderTargetD3D9.Dispose();
                _renderTargetD3D9 = null;
            }

            _d3DImage.Lock();
            _d3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            _d3DImage.Unlock();
        }

        private RenderTarget2D CreateRenderTarget()
        {
            var actualWidth = (int)ActualWidth;
            var actualHeight = (int)ActualHeight;

            if (actualWidth == 0 || actualHeight == 0)
                return null;

            if (GraphicsDevice == null)
                return null;

            var renderTarget = new RenderTarget2D(GraphicsDevice, actualWidth, actualHeight,
                false, SurfaceFormat.Bgra32, DepthFormat.Depth24Stencil8, 1,
                RenderTargetUsage.PlatformContents, true);

            var handle = renderTarget.GetSharedHandle();

            if (handle == IntPtr.Zero)
                throw new ArgumentException("Handle could not be retrieved");

            _renderTargetD3D9 = new SharpDX.Direct3D9.Texture(_graphicsDeviceService.D3DDevice, renderTarget.Width,
                renderTarget.Height,
                1, SharpDX.Direct3D9.Usage.RenderTarget, SharpDX.Direct3D9.Format.A8R8G8B8,
                SharpDX.Direct3D9.Pool.Default, ref handle);

            using (var surface = _renderTargetD3D9.GetSurfaceLevel(0))
            {
                _d3DImage.Lock();
                _d3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                _d3DImage.Unlock();
            }

            return renderTarget;
        }

        private void OnD3DImageIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3DImage.IsFrontBufferAvailable)
                _contentNeedsRefresh = true;
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if ((_contentNeedsRefresh || AlwaysRefresh) && BeginDraw())
            {
                try
                {
                    _d3DImage.Lock();

                    if (_renderTarget == null)
                        _renderTarget = CreateRenderTarget();

                    if (_renderTarget != null)
                    {
                        GraphicsDevice.SetRenderTarget(_renderTarget);
                        SetViewport();
                        Draw?.Invoke(this, new DrawEventArgs(this, _graphicsDeviceService));
                        GraphicsDevice.Flush();
                        _d3DImage.AddDirtyRect(new Int32Rect(0, 0, (int)ActualWidth, (int)ActualHeight));
                    }

                    _contentNeedsRefresh = false;
                }
                finally
                {
                    _d3DImage.Unlock();
                    GraphicsDevice.SetRenderTarget(null);
                }
            }
        }

        private bool BeginDraw()
        {
            // If we have no graphics device, we must be running in the designer.
            if (_graphicsDeviceService == null)
                return false;

            if (!_d3DImage.IsFrontBufferAvailable)
                return false;

            // Make sure the graphics device is big enough, and is not lost.
            if (!HandleDeviceReset())
                return false;

            return true;
        }

        private void SetViewport()
        {
            // Many GraphicsDeviceControl instances can be sharing the same
            // GraphicsDevice. The device backbuffer will be resized to fit the
            // largest of these controls. But what if we are currently drawing
            // a smaller control? To avoid unwanted stretching, we set the
            // viewport to only use the top left portion of the full backbuffer.
            var width = Math.Max(1, (int)ActualWidth);
            var height = Math.Max(1, (int)ActualHeight);
            GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
        }

        private bool HandleDeviceReset()
        {
            if (GraphicsDevice == null)
                return false;

            var deviceNeedsReset = false;

            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    return false;

                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;
            }

            if (deviceNeedsReset)
            {
                //_graphicsDeviceService.ResetDevice((int)ActualWidth, (int)ActualHeight);
                return false;
            }

            return true;
        }

        public void Invalidate()
        {
            _contentNeedsRefresh = true;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            _renderTarget?.Dispose();
            _renderTargetD3D9?.Dispose();
            //_graphicsDeviceService?.Release(disposing);
            IsDisposed = true;
        }

        ~MonoGameDrawingSurface()
        {
            Dispose(false);
        }
    }
}