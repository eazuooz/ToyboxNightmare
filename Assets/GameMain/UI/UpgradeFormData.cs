using System.Collections.Generic;

namespace ToyBoxNightmare
{
    /// <summary>
    /// UpgradeForm에 전달되는 userData.
    /// </summary>
    public class UpgradeFormData
    {
        public List<UpgradeDefinition> Options { get; }

        public UpgradeFormData(List<UpgradeDefinition> options)
        {
            Options = options;
        }
    }
}
