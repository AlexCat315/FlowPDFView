using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using ScaleGestureDetector = Android.Views.ScaleGestureDetector;

namespace Flow.PDFView.Platforms.Android.Common
{
    /// <summary>
    /// en: A RecyclerView with zoom capabilities for displaying PDF pages.
    /// zh: 支持缩放功能的 RecyclerView，用于显示 PDF 页面。
    /// </summary>
    internal partial class ZoomableRecyclerView : RecyclerView
    {
        private const float MinZoom = 1f;
        private const float DefaultMaxZoom = 10f;

        private ScaleGestureDetector? _scaleDetector;
        private float _scaleFactor = MinZoom;

        private float _tranX = 0f;
        private float _tranY = 0f;
        private float _maxTranX = 0f;
        private float _maxTranY = 0f;

        private bool _isScaling;
        private float _lastTouchX;
        private float _lastTouchY;

        public ZoomableRecyclerView(Context context, IAttributeSet? attrs = null, int defStyleAttr = 0) : base(context, attrs, defStyleAttr)
        {
            var scaleListener = new ScaleListener(this);
            _scaleDetector = new ScaleGestureDetector(context, scaleListener);
            SetLayoutManager(new ZoomableLinearLayoutManager(context, LinearLayoutManager.Vertical, false));
            SetItemAnimator(null);
            OverScrollMode = OverScrollMode.Never;
        }

        public bool IsZoomEnabled { get; set; } = true;
        public float MaxZoom { get; set; } = DefaultMaxZoom;
        public bool IsZoomed => _scaleFactor > MinZoom;

        public override bool CanScrollVertically(int direction)
        {
            if (IsZoomed)
                return _tranY > -_maxTranY || _tranY < 0;
            return base.CanScrollVertically(direction);
        }

        public override bool CanScrollHorizontally(int direction)
        {
            if (IsZoomed)
            {
                if (direction < 0)
                    return _tranX < 0;
                return _tranX > -_maxTranX;
            }
            return base.CanScrollHorizontally(direction);
        }

        public int CalculateScrollAmountY(int dy)
        {
            if (dy == 0 || !IsZoomed)
                return dy;
            return (int)(dy / _scaleFactor);
        }

        public int CalculateScrollAmountX(int dx)
        {
            if (dx == 0 || !IsZoomed)
                return dx;
            return (int)(dx / _scaleFactor);
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null)
                return base.OnTouchEvent(e);

            if (!IsZoomEnabled)
                return base.OnTouchEvent(e);

            if (IsZoomed)
            {
                _scaleDetector?.OnTouchEvent(e);
                
                MotionEventActions action = e.ActionMasked;
                
                if (action == MotionEventActions.Up || action == MotionEventActions.Cancel)
                {
                    _isScaling = false;
                    return true;
                }
                
                if (action == MotionEventActions.Down)
                {
                    _lastTouchX = e.GetX();
                    _lastTouchY = e.GetY();
                    return true;
                }
                
                if (action == MotionEventActions.Move && !_isScaling)
                {
                    float x = e.GetX();
                    float y = e.GetY();
                    
                    float dx = _lastTouchX - x;
                    float dy = _lastTouchY - y;
                    
                    HandleScroll(dx, dy);
                    
                    _lastTouchX = x;
                    _lastTouchY = y;
                }
                
                return true;
            }

            _scaleDetector?.OnTouchEvent(e);
            return base.OnTouchEvent(e);
        }

        internal void SetScaling(bool scaling)
        {
            _isScaling = scaling;
        }

        protected override void DispatchDraw(Canvas canvas)
        {
            if (IsZoomed)
            {
                canvas.Save();
                canvas.Translate(_tranX, _tranY);
                canvas.Scale(_scaleFactor, _scaleFactor);
                base.DispatchDraw(canvas);
                canvas.Restore();
            }
            else
            {
                base.DispatchDraw(canvas);
            }
        }

        private void UpdateTransform()
        {
            _maxTranX = Width * _scaleFactor - Width;
            _maxTranY = Height * _scaleFactor - Height;
            
            if (_maxTranX < 0) _maxTranX = 0;
            if (_maxTranY < 0) _maxTranY = 0;
            
            _tranX = Math.Clamp(_tranX, -_maxTranX, 0f);
            _tranY = Math.Clamp(_tranY, -_maxTranY, 0f);
        }

        private void HandleScroll(float distanceX, float distanceY)
        {
            if (IsZoomed)
            {
                _tranX -= distanceX;
                _tranY -= distanceY;
                UpdateTransform();
                Invalidate();
            }
        }

        private void HandleScale(float scaleFactor)
        {
            float oldScale = _scaleFactor;
            _scaleFactor = Math.Clamp(scaleFactor, MinZoom, MaxZoom);
            
            if (Math.Abs(_scaleFactor - oldScale) > 0.001f)
            {
                UpdateTransform();
                Invalidate();
            }
        }

        private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            private readonly ZoomableRecyclerView _view;

            public ScaleListener(ZoomableRecyclerView view)
            {
                _view = view;
            }

            public override bool OnScaleBegin(ScaleGestureDetector detector)
            {
                _view.SetScaling(true);
                return true;
            }

            public override bool OnScale(ScaleGestureDetector detector)
            {
                _view.HandleScale(detector.ScaleFactor * _view._scaleFactor);
                return true;
            }

            public override void OnScaleEnd(ScaleGestureDetector detector)
            {
                _view.SetScaling(false);
            }
        }
    }
}
