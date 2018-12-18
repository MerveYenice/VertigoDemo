using UnityEngine;
using System.Collections;

public class SpawnPointGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private Color _color;
	void OnDrawGizmos()
	{
		// Draw spawn point gizmo  
		Gizmos.color = _color;
		Vector3 startPoint = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
		Gizmos.DrawSphere(startPoint, 1);
	}
#endif
}