namespace GameServer.Logic.Objects
{
    internal class Item : GameObj
    {
        public override GameObjType GameObjType => GameObjType.Item;
        public int TemplateId { get; set; }
    }
}
