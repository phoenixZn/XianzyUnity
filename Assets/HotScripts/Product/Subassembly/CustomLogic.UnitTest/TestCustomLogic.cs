using System.IO;

namespace Xease.CoreGame
{
    public class EntityCustomLogicGenInfo : CustomLogicGenInfo
    {
    }
    

    public class TestCustomLogic
    {
        public CustomLogicService svc = new CustomLogicService();
        CustomLogic logic;

        public TestCustomLogic()
        {
            string resPath = "../../../source/Project.Test/CustomLogicConfig.xml";
            bool isExist = File.Exists(resPath);
            svc.AddConfigContainer(new XmlLogicConfigContainer(LogicContainerKey.LogicConfig_UnitTest_Xml, resPath));
            svc.AddConfigContainer(new LogicConfigs_UnitTest(LogicContainerKey.LogicConfig_UnitTest_CSharp));

            var genInfo = new EntityCustomLogicGenInfo()
            {
                LogicConfigID = 10000,
                ConfigContainerName = LogicContainerKey.LogicConfig_UnitTest_Xml,
            };
            logic = svc.CreateLogic(genInfo);
        }

        public void Update(float dt)
        {
            if (logic == null)
                return;

            logic.Update(dt);

            if (logic.CanStop())
            {
                svc.DestroyLogic(logic);
                logic = null;
            }
        }
    }
}

