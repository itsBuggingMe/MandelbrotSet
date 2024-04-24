using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MandelbrotSet
{
    /// <summary>
    /// Simple Tweening Class, interpolates between two floating point values. For more information, visit https://easings.net/
    /// <br>Contains an optional onEnd Action</br>
    /// </summary>
    internal class Animation
    {
        public bool Delete { get; private set; } = false;
        private KeyFrame[] KeyFrames { get; set; }
        private KeyFrame PrevKeyFrame;

        private float t;

        private Action<float> value;
        private Action OnEnd;

        private ushort UseIndex = 0;

        /// <summary>
        /// Parameter ticks is the length of time in 1/60 second increments. 1 second animation = 60 ticks
        /// </summary>
        public Animation(Action<float> modifier, float startingValue, Action OnEnd, params KeyFrame[] keyFrames)
        {
            if (keyFrames.Length == 0)
                throw new ArgumentException("Animation must have at least one keyframe", nameof(keyFrames));

            value = modifier;
            KeyFrames = keyFrames;
            this.OnEnd = OnEnd;
            PrevKeyFrame = new KeyFrame(startingValue, 0, AnimationType.Linear);
        }
        /// <summary>
        /// Creates an animation without onEnd
        /// </summary>
        public Animation(Action<float> modifier, float startingValue, params KeyFrame[] keyFrames) : this(modifier, startingValue, null, keyFrames)
        {

        }

        /// <summary>
        /// Uses animation to delay until specified duration ends
        /// </summary>
        public Animation(int delay, Action OnEnd) : this(Empty, 0, OnEnd, new KeyFrame(0, delay, AnimationType.None))
        {

        }

        /// <summary>
        /// Animation from 0f, 1f
        /// </summary>
        public Animation(Action<float> modifer, int length, AnimationType animationType) : this(modifer, 0, new KeyFrame(1, length, animationType))
        {

        }

        public void Reset(Action<float> modifier, float startingValue, Action OnEnd, params KeyFrame[] keyFrames)
        {
            if (keyFrames.Length == 0)
                throw new ArgumentException("Animation must have at least one keyframe", nameof(keyFrames));

            value = modifier;
            KeyFrames = keyFrames;
            this.OnEnd = OnEnd;
            PrevKeyFrame = new KeyFrame(startingValue, 0, AnimationType.Linear);
            Delete = false;
            UseIndex = 0;
            t = 0;
        }

        public void AnimationUpdate(GameTime gameTime)
        {
            if (Delete)
                return;

            const float m = 0.06f;
            t += (float)(KeyFrames[UseIndex].TicksRec * gameTime.ElapsedGameTime.TotalMilliseconds * m);

            if (t < 1.0f)
            {
                if (KeyFrames[UseIndex].AnimationType == AnimationType.None)
                    return;
                value(ComputeAnimatedValue());
            }
            else
            {
                value(KeyFrames[UseIndex].EndValue);

                PrevKeyFrame = KeyFrames[UseIndex];

                UseIndex++;

                t = 0;

                if (UseIndex == KeyFrames.Length)
                {
                    Delete = true;
                    OnEnd?.Invoke();
                    return;
                }
            }
        }

        public void SetKeyFrame(int index, KeyFrame keyFrame) => KeyFrames[index] = keyFrame;

        public static Animation GenericAnimation<T>(Action<T> modifier, T startingValue, int ticks, CustomKey<T> keyFrame)
        {
            var p = AnimationPool.Instance.Request();
            p.Reset(f => modifier(CustomKey<T>.ThisTypeLerper(startingValue, keyFrame.EndValue, f)), 0, null, new KeyFrame(1, ticks, keyFrame.AnimationType));
            return p;
        }

        public Animation SetOnEnd(Action onEnd)
        {
            OnEnd = onEnd;
            return this;
        }

        private static Action<float> Empty = (f) => { };

        #region Tweens

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ComputeAnimatedValue()
        {
            return KeyFrames[UseIndex].AnimationType switch
            {
                AnimationType.Linear => t,
                AnimationType.Parabolic => ParabolicInterpolation(t),
                AnimationType.Cubic => CubicInterpolation(t),
                AnimationType.InverseCubic => InverseCubicInterpolation(t),
                AnimationType.Sigmoid => SigmoidInterpolation(t),
                AnimationType.InverseParabolic => InverseParabolicInterpolation(t),
                AnimationType.EaseInOutQuad => EaseInOutQuadInterpolation(t),
                AnimationType.EaseInOutExpo => EaseInOutExpoInterpolation(t),
                AnimationType.EaseInOutQuart => EaseInOutQuartInterpolation(t),
                AnimationType.EaseOutBounce => EaseOutBounceInterpolation(t),
                AnimationType.EaseInOutBounce => EaseInOutBounceInterpolation(t),
                AnimationType.EaseInBounce => EaseInBounceInterpolation(t),
                _ => throw new ArgumentException("No Animation Type implementaion"),
            } * (KeyFrames[UseIndex].EndValue - PrevKeyFrame.EndValue) + PrevKeyFrame.EndValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ParabolicInterpolation(float t) => t * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InverseParabolicInterpolation(float t) => 1 - (1 - t) * (1 - t);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CubicInterpolation(float t) => t * t * t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InverseCubicInterpolation(float t) => 1 - (1 - t) * (1 - t) * (1 - t);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SigmoidInterpolation(float t) => 1 / (1 + MathF.Exp(10 * (-t + 0.5f)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseInOutQuadInterpolation(float x) => x < 0.5 ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseInOutQuartInterpolation(float x) => x < 0.5f ? 8 * x * x * x * x : 1 - MathF.Pow(-2 * x + 2, 4) / 2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseInOutExpoInterpolation(float x) => x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? MathF.Pow(2, 20 * x - 10) / 2 : (2 - MathF.Pow(2, -20 * x + 10)) / 2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseOutBounceInterpolation(float x) => x switch { < 1 / 2.75f => 7.5625f * x * x, < 2 / 2.75f => 7.5625f * (x -= 1.5f / 2.75f) * x + 0.75f, < 2.5f / 2.75f => 7.5625f * (x -= 2.25f / 2.75f) * x + 0.9375f, _ => 7.5625f * (x -= 2.625f / 2.75f) * x + 0.984375f };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseInOutBounceInterpolation(float x) => x < 0.5f ? (1 - EaseOutBounceInterpolation(1 - 2 * x)) / 2 : (1 + EaseOutBounceInterpolation(2 * x - 1)) / 2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseInBounceInterpolation(float x) => 1 - EaseOutBounceInterpolation(1 - x);
        #endregion Tweens
    }

    /// <summary>
    /// Used to detemines the animation style
    /// </summary>
    internal enum AnimationType : byte
    {
        //Original Case
        Linear,
        Parabolic,
        Cubic,
        InverseCubic,

        //Bad one
        Sigmoid,

        //New cast
        InverseParabolic,
        EaseInOutQuad,
        EaseInOutQuart,
        EaseInOutExpo,
        EaseOutBounce,
        EaseInOutBounce,
        EaseInBounce,

        //Wildcard
        None,
    }

    /// <summary>
    /// Represents a single key frame for the animation class
    /// </summary>
    internal struct KeyFrame
    {
        public AnimationType AnimationType;
        public float EndValue;
        public float TicksRec;

        public readonly int Ticks => (int)Math.Round(1 / TicksRec, 0);

        public KeyFrame(float EndValue, int ticks, AnimationType animationType = AnimationType.Linear)
        {
            this.EndValue = EndValue;
            AnimationType = animationType;
            TicksRec = 1f / ticks;
        }
    }

    /// <summary>
    /// Represents a single key frame for the animation class
    /// </summary>
    internal struct CustomKey<T>
    {
        public AnimationType AnimationType;
        public T EndValue;

        public static Func<T, T, float, T> ThisTypeLerper { get; private set; }

        public CustomKey(T endValue, AnimationType animationType = AnimationType.Linear)
        {
            AnimationType = animationType;
            EndValue = endValue;
        }

        public static void InitalizeType(Func<T, T, float, T> lerper)
        {
            ThisTypeLerper = lerper;
        }
    }
}
