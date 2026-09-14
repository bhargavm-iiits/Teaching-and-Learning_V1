using UnityEngine;
namespace ZeroReturn
{
    [CreateAssetMenu(menuName = "Zero Return/Rail Dressing")]
    public class RailDressing : ScriptableObject
    {
        public Material surface;
        public Color background = new Color(.89f, .93f, .91f);
        public Color rail = new Color(.17f, .26f, .23f);
        public Color sphere = new Color(.55f, .65f, .59f);
        public Color bot = new Color(.88f, .89f, .85f);
    }
}
