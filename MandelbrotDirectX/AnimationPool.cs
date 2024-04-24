using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MandelbrotSet
{
    internal class AnimationPool
    {
        public static AnimationPool Instance
        {
            get
            {
                _back ??= new AnimationPool();
                return _back;
            }
        }

        private static AnimationPool _back;

        private Stack<Animation> Availible;
        private List<Animation> Active;

        const int Size = 256;
        private AnimationPool(int size = Size)
        {
            Active = new List<Animation>(size);
            Availible = new Stack<Animation>(size);
            for(int i = 0; i < size; i++)
            {
                Availible.Push(new Animation(null, 0, AnimationType.Linear));
            }
        }

        public void Update(GameTime gameTime)
        {
            for(int i = Active.Count - 1; i >= 0; i--)
            {
                Active[i].AnimationUpdate(gameTime);
                if (Active[i].Delete)
                {
                    Availible.Push(Active[i]);
                    Active.RemoveAt(i);
                }
            }
        }

        public Animation Request()
        {
            var a = Availible.Pop();
            Active.Add(a);
            return a;
        }
    }

    internal static class LogicForwarding
    {
        public static Animation NewAnimation => AnimationPool.Instance.Request();
        public static Animation Create(Action<float> mod, float startV, Action onEnd, params KeyFrame[] frames) 
        {
            var a = NewAnimation;
            a.Reset(mod, startV, onEnd, frames);
            return a;
        }

        public static Animation Create(Action<float> mod, float startV, params KeyFrame[] frames)
        {
            var a = NewAnimation;
            a.Reset(mod, startV, null, frames);
            return a;
        }
        public static Animation ZeroToOne(Action<float> mod, int time, AnimationType type = AnimationType.Linear)
        {
            var a = NewAnimation;
            a.Reset(mod, 0, null, new KeyFrame(1, time, type));
            return a;
        }
        public static Animation Delay(int t)
        {
            Animation animation = NewAnimation;
            animation.Reset(f => { }, 0, null, new KeyFrame(1, t));
            return animation;
        }
    }
}
