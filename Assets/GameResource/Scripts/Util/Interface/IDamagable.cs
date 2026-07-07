namespace Backend.Util.Interface
{
    public interface IDamagable
    {
        bool IsDefeated { get; }
        void TakeDamage(float damage);
    }
}