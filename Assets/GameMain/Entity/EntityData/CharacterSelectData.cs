namespace ToyBoxNightmare
{
    public class CharacterSelectData : EntityData
    {
        public string CharacterKey { get; }

        public CharacterSelectData(int entityId, int typeId, string characterKey)
            : base(entityId, typeId)
        {
            CharacterKey = characterKey;
        }
    }
}
