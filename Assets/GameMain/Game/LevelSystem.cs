using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 경험치 및 레벨 관리. SurvivalGame이 소유한다.
    /// </summary>
    public class LevelSystem
    {
        public int Level    { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int RequiredExp => Level * 100;  // 1레벨=100, 2레벨=200, ...

        public void AddExp(int amount)
        {
            CurrentExp += amount;
            Log.Info("EXP: {0} / {1}", CurrentExp, RequiredExp);

            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                Log.Info("=== LEVEL UP === Lv.{0}", Level);
                GameEntry.GetComponent<EventComponent>().Fire(
                    this, LevelUpEventArgs.Create(Level));
            }
        }
    }
}
