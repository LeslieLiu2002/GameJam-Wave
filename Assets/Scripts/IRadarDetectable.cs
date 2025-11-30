using UnityEngine;

public interface IRadarDetectable
{
    void OnRadarPing(Vector3 sourcePosition, float sourceRangeMax, float sourceRangeSpeed);
}
