using System;
using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.MemoKit.Editor
{
    [Serializable]
    public class MemoLayer
    {
        public string id;
        public string name;
        public bool isVisible;
        public bool isLocked;

        public MemoLayer(string name)
        {
            id = Guid.NewGuid().ToString();
            this.name = name;
            isVisible = true;
            isLocked = false;
        }
    }

    [Serializable]
    public class MemoItem
    {
        public string id;
        public string title;
        public string content;
        public long timestamp;
        public bool isCompleted;
        public bool isPinned;
        public int colorIndex; // 0: Default, 1: Red, 2: Green, 3: Blue, 4: Yellow
        public Vector2 position; // 节点在画布上的位置
        public string nextMemoId; // 指向的下一个节点ID
        public string layerId; // 所属图层ID

        public MemoItem()
        {
            id = Guid.NewGuid().ToString();
            timestamp = DateTime.Now.Ticks;
            title = "新备忘录";
            content = "";
            isCompleted = false;
            colorIndex = 0;
            position = new Vector2(100, 100);
            nextMemoId = "";
            layerId = ""; // Empty means default layer
        }
    }

    [Serializable]
    public class MemoDataList
    {
        public List<MemoItem> items = new List<MemoItem>();
        public List<MemoLayer> layers = new List<MemoLayer>();
        public Vector2 viewOffset = Vector2.zero;
        public float viewZoom = 1.0f;

        public MemoDataList()
        {
            // Ensure at least one default layer exists
            if (layers.Count == 0)
            {
                var defaultLayer = new MemoLayer("默认图层");
                defaultLayer.id = "default"; // Fixed ID for default layer
                layers.Add(defaultLayer);
            }
        }
    }
}
