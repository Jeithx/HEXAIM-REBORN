using UnityEngine;

public interface IDecoyable
{
    bool CanBeDecoyed { get; }
    void OnDecoyStart();
    void OnDecoyEnd();
}