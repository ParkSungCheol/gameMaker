using UnityEngine;
using System.Collections.Generic;

namespace QUGDD
{

public class SpriteGridOrganizer : MonoBehaviour
{
    public int rows = 5;
    public int columns = 5;
    public float spacingX = 2.0f;
    public float spacingY = 2.0f;

    void Start()
    {
        Debug.Log("Starting sprite organization.");
        OrganizeSprites();
    }

    void OrganizeSprites()
    {
        List<GameObject> validChildren = new List<GameObject>();
        CollectValidChildren(transform, validChildren);

        if (validChildren.Count == 0)
        {
            Debug.LogWarning("No valid child objects found.");
            return;
        }

        for (int index = 0; index < validChildren.Count; index++)
        {
            int row = index / columns;
            int column = index % columns;

            Vector3 newPosition = new Vector3(column * spacingX, row * spacingY, 0);
            Debug.Log($"Positioning child {index} at {newPosition}");
            validChildren[index].transform.position = newPosition;
        }
    }

    void CollectValidChildren(Transform parent, List<GameObject> validChildren)
    {
        foreach (Transform child in parent)
        {
            if (child == null)
            {
                Debug.LogWarning("Child transform is null");
                continue;
            }

            GameObject childObject = child.gameObject;

            if (childObject == null)
            {
                Debug.LogWarning("Child object is null");
                continue;
            }

            if (childObject.GetComponent<SpriteRenderer>() != null || childObject.GetComponent<Animator>() != null)
            {
                validChildren.Add(childObject);
            }
            
            // Recursively process the children of this child
            CollectValidChildren(child, validChildren);
        }
    }
}
}