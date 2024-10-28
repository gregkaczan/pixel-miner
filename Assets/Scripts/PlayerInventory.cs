using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

[System.Serializable]
public class SerializationWrapper<T>
{
    public List<T> items;
}

[System.Serializable]
public class InventoryItemData
{
    public string id;
    public Vector2Int pos;
}

namespace Assets.Scripts
{
    [Serializable]
    public class StoredItem
    {
        public ItemDefinition Details;
        public Vector2Int Position;
        public ItemVisual RootVisual;
    }

    public sealed class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance;

        private bool m_IsInventoryReady;
        public List<StoredItem> StoredItems = new List<StoredItem>();

        private VisualElement m_Root;
        private VisualElement m_InventoryGrid;
        public Dimensions InventoryDimensions;

        public static Dimensions SlotDimension { get; private set; }

        private static Label m_ItemDetailHeader;
        private static Label m_ItemDetailBody;
        private static Label m_ItemDetailPrice;

        private VisualElement m_Telegraph;

        /// <summary>
        /// Set the singleton reference and call Configure
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                Configure();
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        private void Start() => LoadInventory();

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Toggle the active state of the target object
                if (m_IsInventoryReady && m_Root != null)
                {
                    if (m_Root.Q<VisualElement>("Container").ClassListContains("hidden"))
                    {
                        m_Root.Q<VisualElement>("Container").RemoveFromClassList("hidden");
                        Configure();
                        ReloadFromSave();
                    }
                    else
                    {
                        m_Root.Q<VisualElement>("Container").AddToClassList("hidden");
                    }
                }
            }
        }

        /// <summary>
        /// Configure the UI by grabbing references and setting necessary properties
        /// </summary>
        private async void Configure()
        {
            //Set references for later
            m_Root = GetComponentInChildren<UIDocument>().rootVisualElement;
            m_InventoryGrid = m_Root.Q<VisualElement>("Grid");
            VisualElement itemDetails = m_Root.Q<VisualElement>("ItemDetails");
            m_ItemDetailHeader = itemDetails.Q<Label>("Header");
            m_ItemDetailBody = itemDetails.Q<Label>("Body");
            m_ItemDetailPrice = itemDetails.Q<Label>("SellPrice");

            // Hack to get move from the grid not the tiny visual element.
            m_Root.RegisterCallback<MouseMoveEvent>(evt => {
                StoredItem item = StoredItems.FirstOrDefault(s => s.RootVisual != null && s.RootVisual.m_IsDragging);
                if (item != null)
                {
                    item.RootVisual.OnMouseMoveEvent(evt);
                }
            });
            m_Root.RegisterCallback<MouseLeaveEvent>(evt => {
                StoredItem item = StoredItems.FirstOrDefault(s => s.RootVisual != null && s.RootVisual.m_IsDragging);
                if (item != null)
                {
                    item.RootVisual.ResetToOriginalPosition();
                }
            });

            ConfigureInventoryTelegraph();

            //Wait for UI toolkit to build UI
            await UniTask.WaitForEndOfFrame();

            ConfigureSlotDimensions();

            //Inventory is now ready
            m_IsInventoryReady = true;
        }

        /// <summary>
        /// Create a visual element for the telegraph (yellow box)
        /// </summary>
        private void ConfigureInventoryTelegraph()
        {

            if (m_Telegraph != null)
            {
                m_Telegraph.RemoveFromHierarchy();
            }

            //Create telegraphing element
            m_Telegraph = new VisualElement
            {
                name = "Telegraph",
            };

            //set style
            m_Telegraph.AddToClassList("slot-icon-highlighted");

            //Add to UI
            AddItemToInventoryGrid(m_Telegraph);
        }

        /// <summary>
        /// Set the single slot dimension based on the total size of the grid)
        /// </summary>
        private void ConfigureSlotDimensions()
        {
            VisualElement firstSlot = m_InventoryGrid.Children().First();

            SlotDimension = new Dimensions
            {
                Width = Mathf.RoundToInt(firstSlot.worldBound.width) + 1,
                Height = Mathf.RoundToInt(firstSlot.worldBound.height)
            };
        }

        private void AddItemToInventoryGrid(VisualElement item) => m_InventoryGrid.Add(item);
        private void RemoveItemFromInventoryGrid(VisualElement item) => m_InventoryGrid.Remove(item);

        private async void LoadInventory()
        {

            LoadInventoryFromFile();

            //make sure inventory is in ready state
            await UniTask.WaitUntil(() => m_IsInventoryReady);

            //load
            foreach (StoredItem loadedItem in StoredItems)
            {
                ItemVisual inventoryItemVisual = new ItemVisual(loadedItem);

                AddItemToInventoryGrid(inventoryItemVisual);

                ConfigureInventoryItem(loadedItem, inventoryItemVisual);
            }

            SaveInventoryToFile();
            
        }

        public List<InventoryItemData> ConvertGridToData(List<StoredItem> storedItems)
        {
            List<InventoryItemData> dataList = new List<InventoryItemData>();

            foreach (var item in storedItems)
            {
                InventoryItemData data = new InventoryItemData();
                data.id = item.Details.ID;
                data.pos = item.Position;
                
                dataList.Add(data);
            }
            return dataList;
        }

        public void SaveInventoryToFile()
        {
            List<InventoryItemData> dataList = ConvertGridToData(StoredItems);
            string jsonData = JsonUtility.ToJson(new SerializationWrapper<InventoryItemData> { items = dataList });

            // Save the JSON data to a file
            System.IO.File.WriteAllText(Application.persistentDataPath + "/ship.json", jsonData);
        }

        public void LoadInventoryFromFile()
        {
            string jsonData;

            if (System.IO.File.Exists(Application.persistentDataPath + "/ship.json"))
            {
                jsonData = System.IO.File.ReadAllText(Application.persistentDataPath + "/ship.json");
            } else {
                jsonData = Resources.Load<TextAsset>("startingShip").text;
            }

            SerializationWrapper<InventoryItemData> wrapper = JsonUtility.FromJson<SerializationWrapper<InventoryItemData>>(jsonData);

            List<StoredItem> loadedItems = new List<StoredItem>();
            
            foreach (var itemData in wrapper.items)
            {
                StoredItem item = new StoredItem
                {
                    Details = Resources.Load<ItemDefinition>($"Data/{itemData.id}"),
                    Position = itemData.pos
                };

                loadedItems.Add(item);
            }
            
            StoredItems = loadedItems;
        }

        /// <summary>
        /// Call to associate item with visual element and set active
        /// </summary>
        private static void ConfigureInventoryItem(StoredItem item, ItemVisual visual)
        {
            item.RootVisual = visual;
            visual.style.visibility = Visibility.Visible;
            SetItemPosition(visual, new Vector2(Mathf.FloorToInt(item.Position.x * SlotDimension.Width), Mathf.FloorToInt(item.Position.y * SlotDimension.Height)));
        }

        /// <summary>
        /// Call to set item properties on UI
        /// </summary>
        /// <param name="item"></param>
        public static void UpdateItemDetails(ItemDefinition item)
        {
            m_ItemDetailHeader.text = item.FriendlyName;
            m_ItemDetailBody.text = item.Description;
        }

        /// <summary>
        /// Set an elements position.
        /// </summary>
        private static void SetItemPosition(VisualElement element, Vector2 vector)
        {
            element.style.left = vector.x;
            element.style.top = vector.y;
        }

        /// <summary>
        /// Finds the position for an item on first load. 
        /// </summary>
        /// <param name="newItem"></param>
        /// <returns></returns>
        private async Task<bool> GetPositionForItem(VisualElement newItem)
        {
            for (int y = 0; y < InventoryDimensions.Height; y++)
            {
                for (int x = 0; x < InventoryDimensions.Width; x++)
                {
                    //try position
                    SetItemPosition(newItem, new Vector2(SlotDimension.Width * x, SlotDimension.Height * y));

                    await UniTask.WaitForEndOfFrame();

                    StoredItem overlappingItem = StoredItems.FirstOrDefault(s => s.RootVisual != null && s.RootVisual.layout.Overlaps(newItem.layout));

                    //Nothing is here! Place the item.
                    if (overlappingItem == null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check to see whether the player can place the item and if so, draw the telegraph.
        /// </summary>
        /// <returns>Whether the item can be placed in the target location and the target location</returns>
        public (bool canPlace, Vector2Int position) ShowPlacementTarget(StoredItem draggedItem)
        {
            //Check to see if it's hanging over the edge - if so, do not place.
            if (!m_InventoryGrid.layout.Contains(new Vector2(draggedItem.RootVisual.localBound.xMax, draggedItem.RootVisual.localBound.yMax)))
            {
                m_Telegraph.style.visibility = Visibility.Hidden;
                return (canPlace: false, position: Vector2Int.zero);
            }

            VisualElement targetSlot = m_InventoryGrid.Children().Where(x => x.layout.Overlaps(draggedItem.RootVisual.layout) && x != draggedItem.RootVisual).OrderBy(x => Vector2.Distance(x.worldBound.position, draggedItem.RootVisual.worldBound.position)).First();

            m_Telegraph.style.width = draggedItem.RootVisual.style.width;
            m_Telegraph.style.height = draggedItem.RootVisual.style.height;

            SetItemPosition(m_Telegraph, new Vector2(targetSlot.layout.position.x, targetSlot.layout.position.y));

            m_Telegraph.style.visibility = Visibility.Visible;

            // Get telegraph position in grid coordinates
            Vector2Int tPos = new Vector2Int(Mathf.RoundToInt(targetSlot.layout.position.x / SlotDimension.Width), Mathf.RoundToInt(targetSlot.layout.position.y / SlotDimension.Height));
            Rect tRect = new Rect(tPos.x, tPos.y, draggedItem.Details.SlotDimension.Width, draggedItem.Details.SlotDimension.Height);

            var overlappingItems = StoredItems.Where(x => {
                Rect r = new Rect(x.Position.x, x.Position.y, x.Details.SlotDimension.Width, x.Details.SlotDimension.Height);
                return r.Overlaps(tRect, true) && x != draggedItem;
            }).ToArray();

            if (overlappingItems.Length > 0)
            {
                m_Telegraph.style.visibility = Visibility.Hidden;
                return (canPlace: false, position: Vector2Int.zero);    
            }

            return (canPlace: true, tPos);

        }

        public void HidePlacementTarget() => m_Telegraph.style.visibility = Visibility.Hidden;

        public void PlaceItem(StoredItem item, Vector2Int position)
        {
            item.Position = position;
            SaveInventoryToFile();
        }

        public void ReloadFromSave()
        {
            foreach (var item in StoredItems)
            {
                item.RootVisual.RemoveFromHierarchy();
            }
            

            StoredItems.Clear();
            LoadInventory();
        }
    }
}
