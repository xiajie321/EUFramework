using System;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.GridView;
using UnityEngine;
using UnityEngine.UI;

namespace EUUI.Extension
{
    public abstract class FrameworkListViewsHolder<TData> : BaseItemViewsHolder
        where TData : class
    {
        public Action<int> OnClicked;

        /// <summary>
        /// 图集 Sprite 加载委托，由 Adapter 在 CreateViewsHolder 时注入
        /// url 格式：atlasName/spriteName
        /// </summary>
        public Func<string, Sprite> SpriteLoader;

        public sealed override void CollectViews()
        {
            base.CollectViews();
            OnCollectViews();
            if (!root.TryGetComponent<Button>(out var clickBtn))
            {
                clickBtn = root.gameObject.AddComponent<Button>();
                clickBtn.transition = Selectable.Transition.None;
            }
            clickBtn.onClick.AddListener(() => OnClicked?.Invoke(ItemIndex));
        }

        protected abstract void OnCollectViews();
        public abstract void OnAcquire(TData data, int index);
        public virtual void OnRelease() { }
    }

    public abstract class FrameworkGridViewsHolder<TData> : CellViewsHolder
        where TData : class
    {
        public Action<int> OnClicked;

        /// <summary>
        /// 图集 Sprite 加载委托，由 Adapter 在 OnCellViewsHolderCreated 时注入
        /// url 格式：atlasName/spriteName
        /// </summary>
        public Func<string, Sprite> SpriteLoader;

        public sealed override void CollectViews()
        {
            base.CollectViews();
            OnCollectViews();

            if (!views.TryGetComponent<Button>(out var clickBtn))
            {
                clickBtn = views.gameObject.AddComponent<Button>();
                clickBtn.transition = Selectable.Transition.None;
            }
            clickBtn.onClick.AddListener(() => OnClicked?.Invoke(ItemIndex));
        }

        protected abstract void OnCollectViews();
        public abstract void OnAcquire(TData data, int index);
        public virtual void OnRelease() { }
    }


}