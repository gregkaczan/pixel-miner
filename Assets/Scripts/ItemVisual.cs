using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class ItemVisual : VisualElement
    {
        private readonly StoredItem m_Item;
        private Vector2 m_OriginalPosition;
        private bool m_IsDragging;

        private (bool canPlace, Vector2 position) m_PlacementResults;

        public ItemVisual(StoredItem item)
        {
            //Set the reference
            m_Item = item;

            //Create a new visual element
            VisualElement icon = new VisualElement
            {
                style = { backgroundImage = m_Item.Details.Icon.texture },
                name = "Icon"
            };

            //Add it as a child of this
            Add(icon);
            //Add a stylesheet for look/feel
            icon.AddToClassList("visual-icon");
            AddToClassList("visual-icon-container");
            
            //Set properties 
            name = $"{m_Item.Details.FriendlyName}";
            style.height = m_Item.Details.SlotDimension.Height * PlayerInventory.SlotDimension.Height;
            style.width = m_Item.Details.SlotDimension.Width * PlayerInventory.SlotDimension.Width;
            style.visibility = Visibility.Hidden;

            //Register the mouse callbacks
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
        }

        private void OnMouseLeaveEvent(MouseLeaveEvent evt)
        {
            throw new NotImplementedException();
        }

        ~ItemVisual()
        {
            UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
        }

        /// <summary>
        /// Gets the mouse position and converts it to the right position relative to the grid part of the UI
        /// </summary>
        /// <param name="mousePosition">Current mouse position</param>
        /// <returns>Converted mouse position relative to the Grid part of the UI</returns>
        public Vector2 GetMousePosition(Vector2 mousePosition) => new Vector2(mousePosition.x - (layout.width / 2) - parent.worldBound.position.x, mousePosition.y - (layout.height / 2) - parent.worldBound.position.y);

        /// <summary>
        /// Set the position of the element
        /// </summary>
        /// <param name="pos">New position</param>
        public void SetPosition(Vector2 pos)
        {
            style.left = pos.x;
            style.top = pos.y;
        }

        /// <summary>
        /// Handles logic for when the mouse has been released
        /// </summary>
        private void OnMouseDownEvent(MouseDownEvent mouseEvent)
        {
            if (!m_IsDragging)
            {
                StartDrag();
                return;
            }

        }

        /// <summary>
        /// Starts the dragging logic
        /// </summary>
        public void StartDrag()
        {
            m_IsDragging = true;
            m_OriginalPosition = worldBound.position - parent.worldBound.position;
            BringToFront();
        }

        /// <summary>
        /// Handles logic for every time the mouse moves. Only runs if the player is actively dragging
        /// </summary>
        private void OnMouseMoveEvent(MouseMoveEvent mouseEvent)
        {
            if (!m_IsDragging)
            { 
                PlayerInventory.UpdateItemDetails(m_Item.Details);
                return; 
            }

            SetPosition(GetMousePosition(mouseEvent.mousePosition));
            m_PlacementResults = PlayerInventory.Instance.ShowPlacementTarget(this);
        }

        private void OnMouseUpEvent(MouseUpEvent mouseEvent)
        {
            m_IsDragging = false;

            if (m_PlacementResults.canPlace)
            {
                SetPosition(new Vector2(
                    m_PlacementResults.position.x - parent.worldBound.position.x,
                    m_PlacementResults.position.y - parent.worldBound.position.y));

                PlayerInventory.Instance.PlaceItem(m_Item, m_PlacementResults.position);

                return;
            }

            SetPosition(new Vector2(m_OriginalPosition.x, m_OriginalPosition.y));
        }
    }
}
