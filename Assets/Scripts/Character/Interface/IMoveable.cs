using UnityEngine;

public interface IMoveable
{
    void Move();
    void Stop();
    void SetDirection(Vector2 direction);
}
