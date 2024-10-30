using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;
using System.Linq;

public class ShipRendererController : MonoBehaviour
{
  
    public GameObject itemPrefab; // Prefab for rendering each ship part

    public void RenderShip(List<StoredItem> storedItems)
    {
        // Clear previous ship parts if re-rendering
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        // Render each part of the ship
        foreach (var storedItem in storedItems)
        {
        // Calculate grid bounds
        int minX = storedItems.Min(item => item.Position.x);
        int minY = storedItems.Min(item => item.Position.y);
        int maxX = storedItems.Max(item => item.Position.x + item.Details.SlotDimension.Width);
        int maxY = storedItems.Max(item => item.Position.y + item.Details.SlotDimension.Height);

        Vector2 gridCenter = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);


        // Create a new GameObject for each part
        GameObject itemObject = Instantiate(itemPrefab, transform);
        itemObject.name = storedItem.Details.FriendlyName;

        // Set position in the grid (adjust based on your grid setup)
        Vector2 centeredPosition = (Vector2)storedItem.Position - gridCenter;
        itemObject.transform.localPosition = new Vector3(
            centeredPosition.x + storedItem.Details.SlotDimension.Width / 2f,
            centeredPosition.y + storedItem.Details.SlotDimension.Height / 2f,
            0);

        // Add a SpriteRenderer and set the sprite to the icon from ItemDefinition
        SpriteRenderer spriteRenderer = itemObject.GetComponent<SpriteRenderer>();
          spriteRenderer.sprite = storedItem.Details.Icon;
        // Add random color to the sprite based on the name
        //spriteRenderer.color = Random.ColorHSV();

        // Adjust scale based on SlotDimension to fit correctly in grid cells
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float scaleX = storedItem.Details.SlotDimension.Width / spriteSize.x;
        float scaleY = storedItem.Details.SlotDimension.Height / spriteSize.y;
        itemObject.transform.localScale = new Vector3(scaleX, scaleY * -1, 1);
        }
    }
}
