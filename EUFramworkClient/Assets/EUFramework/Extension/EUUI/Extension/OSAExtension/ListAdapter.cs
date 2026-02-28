using System;
using System.Collections.Generic;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomParams;
using Com.ForbiddenByte.OSA.CustomAdapters.GridView;
using Com.ForbiddenByte.OSA.DataHelpers;
using UnityEngine;

namespace EUUI.Extension
{
    /// <summary>
    ///  List Adapter
    /// </summary>
    public class FrameworkListAdapter<TData, TVH> : OSA<BaseParamsWithPrefab, TVH>
        where TData : class
        where TVH : FrameworkListViewsHolder<TData>, new()
    {
        public SimpleDataHelper<TData> Data { get; private set; }
        public Action<int, TData> OnItemClick;

        /// <summary>
        /// 图集 Sprite 加载委托，由面板层赋值，创建 VH 时自动注入
        /// url 格式：atlasName/spriteName
        /// </summary>
        public Func<string, Sprite> SpriteLoader;

        public int Count => Data?.Count ?? 0;

        protected override void Start()
        {
            Data = new SimpleDataHelper<TData>(this);
            base.Start();
        }

        protected override TVH CreateViewsHolder(int itemIndex)
        {
            var vh = new TVH();
            vh.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
            vh.OnClicked = idx => OnItemClick?.Invoke(idx, Data[idx]);
            vh.SpriteLoader = SpriteLoader;
            return vh;
        }

        protected override void UpdateViewsHolder(TVH newOrRecycled)
        {
            newOrRecycled.OnAcquire(Data[newOrRecycled.ItemIndex], newOrRecycled.ItemIndex);
        }

        protected override void OnBeforeRecycleOrDisableViewsHolder(TVH inRecycleBinOrVisible, int newItemIndex)
        {
            base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);
            inRecycleBinOrVisible.OnRelease();
        }
        public void ClearItemClickListeners()
        {
            OnItemClick = null;
        }

        #region 数据操作
        public void SetData(IList<TData> items) => Data.ResetItems(items);
        public void AddItem(TData item, bool freezeEndEdge = false)
        {
            Data.InsertItemsAtEnd(new[] { item }, freezeEndEdge);
        }

        public void AddItems(IList<TData> items, bool freezeEndEdge = false)
        {
            Data.InsertItemsAtEnd(items, freezeEndEdge);
        }
        public void InsertAt(int index, TData item) => Data.InsertItems(index, new[] { item });
        public void RemoveAt(int index) => Data.RemoveItems(index, 1);
        public void Clear() => Data.ResetItems(new List<TData>());
        public void RefreshAll() => Data.NotifyListChangedExternally();
        public void RefreshItem(int index) => ForceUpdateViewsHolderIfVisible(index);
        #endregion
    }

    /// <summary>
    ///  Grid Adapter
    /// </summary>
    public class FrameworkGridAdapter<TData, TCellVH> : GridAdapter<GridParams, TCellVH>
        where TData : class
        where TCellVH : FrameworkGridViewsHolder<TData>, new()
    {
        public SimpleDataHelper<TData> Data { get; private set; }
        public event Action<int, TData> OnItemClick;

        /// <summary>
        /// 图集 Sprite 加载委托，由面板层赋值，创建 VH 时自动注入
        /// url 格式：atlasName/spriteName
        /// </summary>
        public Func<string, Sprite> SpriteLoader;

        public int Count => Data?.Count ?? 0;

        protected override void Start()
        {
            Data = new SimpleDataHelper<TData>(this);
            base.Start();
        }

        protected override void OnCellViewsHolderCreated(TCellVH cellVH, CellGroupViewsHolder<TCellVH> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);
            cellVH.OnClicked = idx => OnItemClick?.Invoke(idx, Data[idx]);
            cellVH.SpriteLoader = SpriteLoader;
        }

        protected override void UpdateCellViewsHolder(TCellVH viewsHolder)
        {
            viewsHolder.OnAcquire(Data[viewsHolder.ItemIndex], viewsHolder.ItemIndex);
        }

        protected override void OnBeforeRecycleOrDisableCellViewsHolder(TCellVH viewsHolder, int newItemIndex)
        {
            base.OnBeforeRecycleOrDisableCellViewsHolder(viewsHolder, newItemIndex);
            viewsHolder.OnRelease();
        }
        public void ClearItemClickListeners()
        {
            OnItemClick = null;
        }


        #region 数据操作
        public void SetData(IList<TData> items) => Data.ResetItems(items);
        public void AddItem(TData item, bool freezeEndEdge = false)
        {
            Data.InsertItemsAtEnd(new[] { item }, freezeEndEdge);
        }

        public void AddItems(IList<TData> items, bool freezeEndEdge = false)
        {
            Data.InsertItemsAtEnd(items, freezeEndEdge);
        }
        public void Clear() => Data.ResetItems(new List<TData>());
        public void RefreshAll() => Data.NotifyListChangedExternally();
        public void RefreshItem(int index) => ForceUpdateCellViewsHolderIfVisible(index);
        #endregion
    }
}