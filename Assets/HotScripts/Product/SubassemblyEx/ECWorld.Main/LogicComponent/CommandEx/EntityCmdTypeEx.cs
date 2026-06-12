namespace Xease.CoreGame
{
    public static partial class EntityCmdType
    {
        public const int Op_MoveDir = 1;
        public const int Op_MoveDirRelease = 2;
        public const int Op_A_Press = 3;
        public const int Op_A_Release = 4;
        public const int Op_B_Press = 5;
        public const int Op_B_Release = 6;
        public const int Op_X_Press = 7;
        public const int Op_X_Release = 8;
        public const int Op_Y_Press = 9;
        public const int Op_Y_Release = 10;
        
        public const int Nt_ColliderHit = 101;
        public const int Nt_Death = 103;
        public const int Nt_OnHurt = 104;
    }
    
    public partial struct EntityCommand
    {
        public long EntityID { get; set; }
        //命令数据:
        public AnyValve V0;
        public AnyValve V1;
        public AnyValve V2;
        public AnyValve V3;
    }
}