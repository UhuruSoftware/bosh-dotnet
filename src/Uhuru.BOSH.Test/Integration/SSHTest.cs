using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Message;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Integration
{
    [TestClass]
    public class SSHTest
    {
        [TestMethod]
        public void TC001_TestSSH()
        {
            //Arrange
            Ssh ssh = new Ssh();
            string sshMessage = "[\"setup\",{\"user\":\"bosh_ircor23u3\",\"public_key\":\"ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQCr8DjwYoyfnhth18sfS8FBd+sjLZ8TShaHmWYTUKLqsVs/XR35ZKDrzwwEsDCva01drwQr3/ynkznVdycCnqi9cSRA+f1McOlKU9KGLEFx8tusRpQbOkF8bmFJqdG9BZBZnN6wEI+s1n8vU1xjWjM1vw/Wc1ad+VuKd3wCvgCC8waUCug47+OxDJtSH088Ex2WzZ59o6JVBl6v4PHWeKuaUYkndTgpQxmDZd3Z9q1TbGOxdmt6JVRvL43QUXSiOJfvvszKVxk0TU1dtYvNDd9JyST7988RezEPHxVeBdVbSJDeV/logaRGyZwGO6DpTP1hObXRD/gxoirTJ/k41n3L root@ubuntu\n\",\"password\":\"yiXHuTQwaXOiI\"}]";
            
           //Act
            ssh.Process(JsonConvert.DeserializeObject(sshMessage));

            //Assert
        }
    }
}
