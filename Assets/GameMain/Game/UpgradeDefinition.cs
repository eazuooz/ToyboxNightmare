using System;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 레벨업 시 선택 가능한 업그레이드 하나를 나타낸다.
    /// </summary>
    public class UpgradeDefinition
    {
        public string Name        { get; }
        public string Description { get; }

        private readonly Action mApply;

        public UpgradeDefinition(string name, string description, Action apply)
        {
            Name        = name;
            Description = description;
            mApply      = apply;
        }

        public void Apply() => mApply?.Invoke();
    }
}
