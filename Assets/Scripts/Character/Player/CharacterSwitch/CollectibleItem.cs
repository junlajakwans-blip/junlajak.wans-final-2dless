using UnityEngine;

[System.Serializable]
public class CollectibleItem
{
    [SerializeField] private string _itemID;
    [SerializeField] private CollectibleType _type;
    [SerializeField] private int _value;
    [SerializeField] private Sprite _icon;

    public string ItemID => _itemID;
    public CollectibleType Type => _type;
    public int Value => _value;
    public Sprite Icon => _icon;

    public void OnCollected(Player player)
    {
        Debug.Log($"Player collected item: {_itemID}");
        ApplyEffect(player);
    }

    public void ApplyEffect(Player player)
    {
        switch (_type)
        {
            case CollectibleType.Coin:
                Debug.Log($"Add {_value} coins to player");
                break;

            case CollectibleType.Health:
                Debug.Log($"Restore {_value} HP");
                break;

            case CollectibleType.CareerUnlock:
                Debug.Log("Unlock new career");
                break;

            default:
                Debug.Log("Unknown collectible type");
                break;
        }
    }
}
