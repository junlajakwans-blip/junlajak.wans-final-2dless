public interface IDamageable
{
    void Initialize(int maxHealth);
    void TakeDamage(int amount);
    void Heal(int amount);
    void Die();
}
