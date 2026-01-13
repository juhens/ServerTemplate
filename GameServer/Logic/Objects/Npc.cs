namespace GameServer.Logic.Objects
{
    internal class Npc : GameObj
    {
        public override GameObjType GameObjType => GameObjType.Npc;
        public int TemplateId { get; set; }

    }
}
