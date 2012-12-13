using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IronRuby;
using System.IO;
using Uhuru.BOSH.Agent.Ruby;
using Uhuru.BOSH.Agent.ApplyPlan;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class RubyErbTests
    {
        [TestMethod]
        //[DeploymentItem(@"Ruby\erb.rb")]
        [DeploymentItem(@"Ruby\erb.rb", @"Ruby")]
        [DeploymentItem(@"..\..\lib\IronRuby.JsonExt.dll")]
        [DeploymentItem(@"..\..\lib\IronRuby.Libraries.dll")]
        [DeploymentItem(@"..\..\lib\IronRuby.Libraries.Yaml.dll")]
        public void TC001_TestRuby()
        {
            //Arrange
            var engine = Ruby.CreateEngine();
            
            //Act
            dynamic result = engine.Execute(" 'test' ");

            //Assert
            Assert.AreEqual(result.ToString(), "test");
        }

        [TestMethod]
        public void TC002_TestRubyVars()
        {
            //Arrange
            var engine = Ruby.CreateEngine();
            var scope = engine.CreateScope();
            scope.SetVariable("myVar", "test");

            //Act
            dynamic result = engine.Execute(" myVar ", scope);

            //Assert
            Assert.AreEqual(result.ToString(), "test");
        }

        [TestMethod]
        public void TC003_TestERB()
        {
            //Arrange
            string template = "<%= x %>";

            var engine = Ruby.CreateEngine();
            var scope = engine.CreateScope();
            scope.SetVariable("templateText", template);
            
            //Act
            dynamic result = engine.Execute(@"
            require 'Ruby\erb.rb'
    
            x = 'test'
            
            template = ERB.new(templateText.to_s)
            res = template.result(binding)
            ", scope);

            //Assert
            Assert.AreEqual(result.ToString().Trim(), "test");
        }

        [TestMethod]
        [DeploymentItem(@"Ruby", @"Ruby")]
        [DeploymentItem(@"Resources\WinServBosh.exe.config.erb")]
        public void TC004_TestERBWithFile()
        {
            //Arrange
            ErbTemplate erbTemplate = new ErbTemplate();
            string currentProp = @"{boshtest: {outputfile: ""c:\\test.txt"",testmessage: ""this is a test""}}";

            //Act
            string result = erbTemplate.Execute("WinServBosh.exe.config.erb", currentProp);
            
            //Assert
            Assert.AreNotEqual(result, string.Empty);
        }

        [TestMethod]
        [DeploymentItem(@"Ruby", @"Ruby")]
        [DeploymentItem(@"Resources\WinServBosh.exe.config.erb")]
        public void TC005_TestERBWithFile()
        {
            //Ignore this for now
            //Arrange
            ErbTemplate erbTemplate = new ErbTemplate();
            //string currentProp = @"{:""deployment""=> ""cloud-foundry"", :""release""=> {:""name""=> ""uhuru-appcloud"", :""version""=> ""122.1-dev""}, :""job""=> {:""name""=> ""win_dea"", :""release""=> ""uhuru-appcloud"", :""templates""=> {}, :""template""=> ""win_dea"", :""version""=> ""0.1-dev"", :""sha1""=> ""7b265cacae4076807b9414b71c3b3e44b37b133e"", :""blobstore_id""=> ""67538bd7-3542-4d96-a355-2d3e5f6b27e7""}, :""index""=> ""0"", :""networks""=> {:""default""=> {:""ip""=> ""10.134.1.37"", :""netmask""=> ""255.255.0.0"", :""cloud_properties""=> {:""name""=> ""VM Network""}, :""default""=> {}, :""dns""=> {}, :""gateway""=> ""10.134.0.1""}}, :""resource_pool""=> {:""name""=> ""windows"", :""cloud_properties""=> {:""ram""=> ""900"", :""disk""=> ""5000"", :""cpu""=> ""2""}, :""stemcell""=> {:""name""=> ""uhuru-windows-2008R2"", :""version""=> ""0.9.2""}}, :""packages""=> {:""win_dea""=> {:""name""=> ""win_dea"", :""version""=> ""0.1-dev.1"", :""sha1""=> ""66E9F9E5F4ADC7F545EE9DF5507A19FA72F2C799"", :""blobstore_id""=> ""87df05e5-9fc5-42e4-89c2-5fb5066cc642""}}, :""persistent_disk""=> ""0"", :""configuration_hash""=> ""3f4e03e97d9154fa212b5e126a929367ee479434"", :""properties""=> {:""domain""=> ""testcfm.com"", :""env""=> {}, :""networks""=> {:""apps""=> ""default"", :""management""=> ""default""}, :""uhuru""=> {:""simple_webui""=> {:""port""=> ""9191"", :""cloud_name""=> ""Uhuru Cloud"", :""domain""=> ""cf.me"", :""recaptcha_public_key""=> ""6Lfqe9kSAAAAAITuzJDfgLX41pMhyj2UEDp72NiM"", :""recaptcha_private_key""=> ""6Lfqe9kSAAAAAMKol6bOmzfghgan53CIG9ftzYi-"", :""activation_link_secret""=> ""casnuugycasuygc12tddbhcb7"", :""email""=> {:""from""=> ""uhurusoftw@gmail.com"", :""from_alias""=> ""Uhuru Cloud""}, :""contact""=> {:""email""=> ""uhuru@uhurusoftware.com""}, :""admin_email""=> ""dev@uhurusoftware.com"", :""admin_password""=> ""password1234!"", :""default_random_password""=> ""01234567890123456789""}}, :""win_dea""=> {:""filerport""=> ""12346"", :""statusport""=> ""12345"", :""max_memory""=> ""4096""}, :""uhurufs_gateway""=> {:""check_orphan_interval""=> ""7200"", :""token""=> ""0xfeedface"", :""service_timeout""=> ""60"", :""node_timeout""=> ""30"", :""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""0.9""}}, :""uhurufs_node""=> {:""capacity""=> ""200"", :""statusport""=> ""12345"", :""default_version""=> ""0.9""}, :""mssql_gateway""=> {:""check_orphan_interval""=> ""7200"", :""token""=> ""0xfeedface"", :""service_timeout""=> ""60"", :""node_timeout""=> ""30"", :""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""2008""}}, :""mssql_node""=> {:""capacity""=> ""200"", :""admin_user""=> ""sa"", :""admin_password""=> ""changem3!"", :""port""=> ""1433"", :""max_db_size""=> ""20"", :""max_long_query""=> ""3"", :""max_long_tx""=> ""30"", :""max_user_conns""=> ""20"", :""default_version""=> ""2008"", :""statusport""=> ""12345""}, :""nats""=> {:""user""=> ""olCiM3B83fOB"", :""password""=> ""IaSmu0AeI2ky"", :""address""=> ""10.134.0.154"", :""port""=> ""4222""}, :""ccdb""=> {:""user""=> ""root"", :""password""=> ""EX0Ii05h5RAv"", :""address""=> ""10.134.0.158"", :""port""=> ""5432"", :""pool_size""=> ""10"", :""dbname""=> ""3JKMvxNowYva0DVO"", :""databases""=> {}, :""roles""=> {}}, :""cc""=> {:""srv_api_uri""=> ""api.uhurucloud.net"", :""password""=> ""qOuQjgV6sMfTcU7f"", :""token""=> ""RcfPRmNycNzocD0n"", :""use_nginx""=> ""True"", :""new_stager_percent""=> ""100"", :""staging_upload_user""=> ""kFs4lM95"", :""staging_upload_password""=> ""8XIseKVt"", :""allow_registration""=> ""True"", :""allow_external_app_uris""=> ""True"", :""admins""=> {}}, :""vcap_redis""=> {:""address""=> ""10.134.0.160"", :""port""=> ""5454"", :""password""=> ""ncwCU3QkgtjZlMTX"", :""maxmemory""=> ""1000000000""}, :""router""=> {:""client_inactivity_timeout""=> ""120"", :""app_inactivity_timeout""=> ""120"", :""status""=> {:""port""=> ""8080"", :""user""=> ""aGz9wG1o2JUR"", :""password""=> ""PsWOrUcl8C7O""}}, :""dashboard""=> {:""uaa""=> {:""client_id""=> ""dashboard"", :""client_secret""=> ""secret""}}, :""dea""=> {:""max_memory""=> ""12288""}, :""nfs_server""=> {:""address""=> ""10.134.0.152"", :""network""=> ""10.134.0.0/16""}, :""hbase_master""=> {:""address""=> ""10.134.0.155"", :""hbase_master""=> {:""port""=> ""60000"", :""webui_port""=> ""60010"", :""heap_size""=> ""768""}, :""hbase_zookeeper""=> {:""heap_size""=> ""768""}, :""hadoop_namenode""=> {:""port""=> ""9000""}}, :""hbase_slave""=> {:""hbase_regionserver""=> {:""port""=> ""60020"", :""heap_size""=> ""768""}, :""addresses""=> {}}, :""opentsdb""=> {:""address""=> ""10.134.0.157"", :""port""=> ""4242""}, :""service_plans""=> {:""mysql""=> {:""free""=> {:""job_management""=> {:""high_water""=> ""1400"", :""low_water""=> ""100""}, :""configuration""=> {:""allow_over_provisioning""=> ""200"", :""capacity""=> ""3"", :""max_db_size""=> ""30"", :""max_long_query""=> ""20"", :""max_long_tx""=> ""0"", :""max_clients""=> ""20""}}, :""nonfree""=> {:""job_management""=> {:""high_water""=> ""2800"", :""low_water""=> ""200""}, :""configuration""=> {:""allow_over_provisioning""=> ""400"", :""capacity""=> ""6"", :""max_db_size""=> ""60"", :""max_long_query""=> ""40"", :""max_long_tx""=> ""0"", :""max_clients""=> ""40""}}, :""gold""=> {:""job_management""=> {:""high_water""=> ""6800"", :""low_water""=> ""300""}, :""configuration""=> {:""allow_over_provisioning""=> ""600"", :""capacity""=> ""9"", :""max_db_size""=> ""300"", :""max_long_query""=> ""200"", :""max_long_tx""=> ""0"", :""max_clients""=> ""200""}}}, :""postgresql""=> {:""free""=> {:""job_management""=> {:""high_water""=> ""1400"", :""low_water""=> ""100""}, :""configuration""=> {:""capacity""=> ""200"", :""max_db_size""=> ""128"", :""max_long_query""=> ""3"", :""max_long_tx""=> ""30"", :""max_clients""=> ""20""}}, :""nonfree""=> {:""job_management""=> {:""high_water""=> ""2800"", :""low_water""=> ""100""}, :""configuration""=> {:""capacity""=> ""200"", :""max_db_size""=> ""128"", :""max_long_query""=> ""3"", :""max_long_tx""=> ""30"", :""max_clients""=> ""20""}}, :""gold""=> {:""job_management""=> {:""high_water""=> ""1400"", :""low_water""=> ""100""}, :""configuration""=> {:""capacity""=> ""200"", :""max_db_size""=> ""128"", :""max_long_query""=> ""3"", :""max_long_tx""=> ""30"", :""max_clients""=> ""20""}}}, :""mongodb""=> {:""free""=> {:""job_management""=> {:""high_water""=> ""3000"", :""low_water""=> ""100""}, :""configuration""=> {:""allow_over_provisioning""=> ""True"", :""capacity""=> ""200"", :""quota_files""=> ""4"", :""max_clients""=> ""500""}}}, :""redis""=> {:""free""=> {:""job_management""=> {:""high_water""=> ""1400"", :""low_water""=> ""100""}, :""configuration""=> {:""capacity""=> ""200"", :""max_memory""=> ""16"", :""max_swap""=> ""32"", :""max_clients""=> ""500""}}}, :""rabbit""=> {:""free""=> {:""job_management""=> {:""low_water""=> ""100"", :""high_water""=> ""1400""}, :""configuration""=> {:""max_memory_factor""=> ""0.5"", :""max_clients""=> ""512"", :""capacity""=> ""200""}}}, :""mssql""=> {:""free""=> {:""job_management""=> {:""high_water""=> ""1400"", :""low_water""=> ""100""}, :""configuration""=> {:""allow_over_provisioning""=> ""True"", :""capacity""=> ""200"", :""max_db_size""=> ""128"", :""max_long_query""=> ""3"", :""max_long_tx""=> ""0"", :""max_clients""=> ""20"", :""lifecycle""=> {:""enable""=> ""True"", :""worker_count""=> ""1"", :""snapshot""=> {:""quota""=> ""10""}}}}}, :""uhurufs""=> {:""free""=> {:""job_management""=> {:""high_water""=> ""1400"", :""low_water""=> ""100""}, :""configuration""=> {:""capacity""=> ""200"", :""max_memory""=> ""16"", :""max_swap""=> ""32"", :""max_clients""=> ""500""}}}}, :""mysql_gateway""=> {:""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""5.1""}, :""check_orphan_interval""=> ""7200"", :""token""=> ""Alg82gD2mXjtyq5B"", :""node_timeout""=> ""60"", :""service_timeout""=> ""30""}, :""mysql_node""=> {:""supported_versions""=> {}, :""default_versions""=> ""5.1"", :""production""=> ""True"", :""password""=> ""aaa13djkas"", :""op_time_limit""=> ""50""}, :""redis_gateway""=> {:""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""2.2""}, :""token""=> ""gQ1mjpqdzhljRGju"", :""check_orphan_interval""=> ""7200"", :""node_timeout""=> ""60"", :""service_timeout""=> ""30""}, :""redis_node""=> {:""supported_versions""=> {}, :""default_versions""=> ""2.2"", :""op_time_limit""=> ""50""}, :""mongodb_gateway""=> {:""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""2.0"", :""deprecated""=> ""1.8""}, :""check_orphan_interval""=> ""7200"", :""token""=> ""ELcjgTsd7Tsfpcnz"", :""node_timeout""=> ""60"", :""service_timeout""=> ""30""}, :""mongodb_node""=> {:""supported_versions""=> {}, :""default_version""=> ""1.8"", :""op_time_limit""=> ""50""}, :""rabbit_gateway""=> {:""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""2.4""}, :""check_orphan_interval""=> ""7200"", :""token""=> ""9rxCftgnofi0GXZ6"", :""node_timeout""=> ""60"", :""service_timeout""=> ""30""}, :""rabbit_node""=> {:""supported_versions""=> {}, :""default_versions""=> ""2.4"", :""op_time_limit""=> ""50""}, :""postgresql_gateway""=> {:""check_orphan_interval""=> ""7200"", :""token""=> ""L01xwjdGIhHH0I4L"", :""supported_versions""=> {}, :""version_aliases""=> {:""current""=> ""9.0""}}, :""postgresql_node""=> {:""production""=> ""True"", :""supported_versions""=> {}, :""default_version""=> ""9.0""}, :""syslog_aggregator""=> {:""address""=> ""10.134.0.153"", :""port""=> ""54321""}, :""report_processor""=> {:""syslog_aggregator""=> {:""address""=> ""10.134.0.153"", :""port""=> ""54321""}}, :""stager""=> {:""max_staging_duration""=> ""600"", :""max_active_tasks""=> ""10"", :""queues""=> {}}, :""uaadb""=> {:""address""=> ""10.134.0.159"", :""port""=> ""2544"", :""roles""=> {}, :""databases""=> {}}, :""uaa""=> {:""cc""=> {:""token_secret""=> ""K8ZzbApdyQCQ"", :""client_secret""=> ""pGxqqbLhpZCX""}, :""admin""=> {:""client_secret""=> ""x6PJgtTTfiX5""}, :""login""=> {:""client_secret""=> ""LqhuTbXHeRZP""}, :""clients""=> {:""dashboard""=> {:""id""=> ""dashboard"", :""secret""=> ""xKlVoYLgXBFU"", :""authorized-grant-types""=> ""authorization_code,refresh_token"", :""scope""=> ""openid,dashboard.user"", :""authorities""=> ""uaa.resource,tokens.read,tokens.write"", :""access-token-validity""=> ""5000""}, :""dashborad_admin""=> {:""id""=> ""dashboard_admin"", :""secret""=> ""somesecret"", :""authorized-grant-types""=> ""client_credentials"", :""scope""=> ""uaa.none"", :""authorities""=> ""scim.read,scim.write,tokens.read,tokens.write,clients.read,clients.write,uaa.admin"", :""access-token-validity""=> ""5000""}}, :""scim""=> {:""users""=> {}}}}, :""template_hashes""=> {:""win_dea""=> ""1e4703bf82d15bf96245acd50e8628a6994fa05a""}}";

            //Act
            //string result = erbTemplate.Execute(@"E:\_me\testerb\uhuru.config.erb", currentProp);

            //Assert
            //Assert.AreNotEqual(result, string.Empty);
        }

        [TestMethod]
        public void TC006_TestRubyObject()
        {
            //Job testJob = new Job(null, null);
            string currentProp = @"{
  ""deployment"": ""cloud-foundry"",
  ""release"": {
    ""name"": ""uhuru-appcloud"",
    ""version"": ""122.1-dev""
  },
  ""job"": {
    ""name"": ""win_dea"",
    ""release"": ""uhuru-appcloud"",
    ""templates"": [
      {
        ""name"": ""win_dea"",
        ""version"": ""0.1-dev"",
        ""sha1"": ""7b265cacae4076807b9414b71c3b3e44b37b133e"",
        ""blobstore_id"": ""67538bd7-3542-4d96-a355-2d3e5f6b27e7""
      }
    ],
    ""template"": ""win_dea"",
    ""version"": ""0.1-dev"",
    ""sha1"": ""7b265cacae4076807b9414b71c3b3e44b37b133e"",
    ""blobstore_id"": ""67538bd7-3542-4d96-a355-2d3e5f6b27e7""
  },
  ""index"": 0,
  ""networks"": {
    ""default"": {
      ""ip"": ""10.134.1.37"",
      ""netmask"": ""255.255.0.0"",
      ""cloud_properties"": {
        ""name"": ""VM Network""
      },
      ""default"": [
        ""dns"",
        ""gateway""
      ],
      ""dns"": [
        ""192.168.1.130"",
        ""8.8.8.8""
      ],
      ""gateway"": ""10.134.0.1""
    }
  },
  ""resource_pool"": {
    ""name"": ""windows"",
    ""cloud_properties"": {
      ""ram"": 900,
      ""disk"": 5000,
      ""cpu"": 2
    },
    ""stemcell"": {
      ""name"": ""uhuru-windows-2008R2"",
      ""version"": ""0.9.2""
    }
  },
  ""packages"": {
    ""win_dea"": {
      ""name"": ""win_dea"",
      ""version"": ""0.1-dev.1"",
      ""sha1"": ""66E9F9E5F4ADC7F545EE9DF5507A19FA72F2C799"",
      ""blobstore_id"": ""87df05e5-9fc5-42e4-89c2-5fb5066cc642""
    }
  },
  ""persistent_disk"": 0,
  ""configuration_hash"": ""3f4e03e97d9154fa212b5e126a929367ee479434"",
  ""properties"": {
    ""domain"": ""testcfm.com"",
    ""env"": {},
    ""networks"": {
      ""apps"": ""default"",
      ""management"": ""default""
    },
    ""uhuru"": {
      ""simple_webui"": {
        ""port"": 9191,
        ""cloud_name"": ""Uhuru Cloud"",
        ""domain"": ""cf.me"",
        ""recaptcha_public_key"": ""6Lfqe9kSAAAAAITuzJDfgLX41pMhyj2UEDp72NiM"",
        ""recaptcha_private_key"": ""6Lfqe9kSAAAAAMKol6bOmzfghgan53CIG9ftzYi-"",
        ""activation_link_secret"": ""casnuugycasuygc12tddbhcb7"",
        ""email"": {
          ""from"": ""uhurusoftw@gmail.com"",
          ""from_alias"": ""Uhuru Cloud""
        },
        ""contact"": {
          ""email"": ""uhuru@uhurusoftware.com""
        },
        ""admin_email"": ""dev@uhurusoftware.com"",
        ""admin_password"": ""password1234!"",
        ""default_random_password"": ""01234567890123456789""
      }
    },
    ""win_dea"": {
      ""filerport"": 12346,
      ""statusport"": 12345,
      ""max_memory"": 4096
    },
    ""uhurufs_gateway"": {
      ""check_orphan_interval"": 7200,
      ""token"": ""0xfeedface"",
      ""service_timeout"": 60,
      ""node_timeout"": 30,
      ""supported_versions"": [
        ""0.9""
      ],
      ""version_aliases"": {
        ""current"": ""0.9""
      }
    },
    ""uhurufs_node"": {
      ""capacity"": 200,
      ""statusport"": 12345,
      ""default_version"": 0.9
    },
    ""mssql_gateway"": {
      ""check_orphan_interval"": 7200,
      ""token"": ""0xfeedface"",
      ""service_timeout"": 60,
      ""node_timeout"": 30,
      ""supported_versions"": [
        ""2008""
      ],
      ""version_aliases"": {
        ""current"": ""2008""
      }
    },
    ""mssql_node"": {
      ""capacity"": 200,
      ""admin_user"": ""sa"",
      ""admin_password"": ""changem3!"",
      ""port"": 1433,
      ""max_db_size"": 20,
      ""max_long_query"": 3,
      ""max_long_tx"": 30,
      ""max_user_conns"": 20,
      ""default_version"": 2008,
      ""statusport"": 12345
    },
    ""nats"": {
      ""user"": ""olCiM3B83fOB"",
      ""password"": ""IaSmu0AeI2ky"",
      ""address"": ""10.134.0.154"",
      ""port"": 4222
    },
    ""ccdb"": {
      ""user"": ""root"",
      ""password"": ""EX0Ii05h5RAv"",
      ""address"": ""10.134.0.158"",
      ""port"": 5432,
      ""pool_size"": 10,
      ""dbname"": ""3JKMvxNowYva0DVO"",
      ""databases"": [
        {
          ""tag"": ""cc"",
          ""name"": ""fuCgaSL1Kva4""
        }
      ],
      ""roles"": [
        {
          ""tag"": ""admin"",
          ""name"": ""root"",
          ""password"": ""8KMxmgjuWrl1""
        }
      ]
    },
    ""cc"": {
      ""srv_api_uri"": ""api.uhurucloud.net"",
      ""password"": ""qOuQjgV6sMfTcU7f"",
      ""token"": ""RcfPRmNycNzocD0n"",
      ""use_nginx"": true,
      ""new_stager_percent"": 100,
      ""staging_upload_user"": ""kFs4lM95"",
      ""staging_upload_password"": ""8XIseKVt"",
      ""allow_registration"": true,
      ""allow_external_app_uris"": true,
      ""admins"": [
        ""b976ef56-27f2-44a7-98d7-96706883f0b4@uhurucloud.net""
      ]
    },
    ""vcap_redis"": {
      ""address"": ""10.134.0.160"",
      ""port"": 5454,
      ""password"": ""ncwCU3QkgtjZlMTX"",
      ""maxmemory"": 1000000000
    },
    ""router"": {
      ""client_inactivity_timeout"": 120,
      ""app_inactivity_timeout"": 120,
      ""status"": {
        ""port"": 8080,
        ""user"": ""aGz9wG1o2JUR"",
        ""password"": ""PsWOrUcl8C7O""
      }
    },
    ""dashboard"": {
      ""uaa"": {
        ""client_id"": ""dashboard"",
        ""client_secret"": ""secret""
      }
    },
    ""dea"": {
      ""max_memory"": 12288
    },
    ""nfs_server"": {
      ""address"": ""10.134.0.152"",
      ""network"": ""10.134.0.0/16""
    },
    ""hbase_master"": {
      ""address"": ""10.134.0.155"",
      ""hbase_master"": {
        ""port"": 60000,
        ""webui_port"": 60010,
        ""heap_size"": 768
      },
      ""hbase_zookeeper"": {
        ""heap_size"": 768
      },
      ""hadoop_namenode"": {
        ""port"": 9000
      }
    },
    ""hbase_slave"": {
      ""hbase_regionserver"": {
        ""port"": 60020,
        ""heap_size"": 768
      },
      ""addresses"": [
        ""10.134.0.156""
      ]
    },
    ""opentsdb"": {
      ""address"": ""10.134.0.157"",
      ""port"": 4242
    },
    ""service_plans"": {
      ""mysql"": {
        ""free"": {
          ""job_management"": {
            ""high_water"": 1400,
            ""low_water"": 100
          },
          ""configuration"": {
            ""allow_over_provisioning"": 200,
            ""capacity"": 3,
            ""max_db_size"": 30,
            ""max_long_query"": 20,
            ""max_long_tx"": 0,
            ""max_clients"": 20
          }
        },
        ""nonfree"": {
          ""job_management"": {
            ""high_water"": 2800,
            ""low_water"": 200
          },
          ""configuration"": {
            ""allow_over_provisioning"": 400,
            ""capacity"": 6,
            ""max_db_size"": 60,
            ""max_long_query"": 40,
            ""max_long_tx"": 0,
            ""max_clients"": 40
          }
        },
        ""gold"": {
          ""job_management"": {
            ""high_water"": 6800,
            ""low_water"": 300
          },
          ""configuration"": {
            ""allow_over_provisioning"": 600,
            ""capacity"": 9,
            ""max_db_size"": 300,
            ""max_long_query"": 200,
            ""max_long_tx"": 0,
            ""max_clients"": 200
          }
        }
      },
      ""postgresql"": {
        ""free"": {
          ""job_management"": {
            ""high_water"": 1400,
            ""low_water"": 100
          },
          ""configuration"": {
            ""capacity"": 200,
            ""max_db_size"": 128,
            ""max_long_query"": 3,
            ""max_long_tx"": 30,
            ""max_clients"": 20
          }
        },
        ""nonfree"": {
          ""job_management"": {
            ""high_water"": 2800,
            ""low_water"": 100
          },
          ""configuration"": {
            ""capacity"": 200,
            ""max_db_size"": 128,
            ""max_long_query"": 3,
            ""max_long_tx"": 30,
            ""max_clients"": 20
          }
        },
        ""gold"": {
          ""job_management"": {
            ""high_water"": 1400,
            ""low_water"": 100
          },
          ""configuration"": {
            ""capacity"": 200,
            ""max_db_size"": 128,
            ""max_long_query"": 3,
            ""max_long_tx"": 30,
            ""max_clients"": 20
          }
        }
      },
      ""mongodb"": {
        ""free"": {
          ""job_management"": {
            ""high_water"": 3000,
            ""low_water"": 100
          },
          ""configuration"": {
            ""allow_over_provisioning"": true,
            ""capacity"": 200,
            ""quota_files"": 4,
            ""max_clients"": 500
          }
        }
      },
      ""redis"": {
        ""free"": {
          ""job_management"": {
            ""high_water"": 1400,
            ""low_water"": 100
          },
          ""configuration"": {
            ""capacity"": 200,
            ""max_memory"": 16,
            ""max_swap"": 32,
            ""max_clients"": 500
          }
        }
      },
      ""rabbit"": {
        ""free"": {
          ""job_management"": {
            ""low_water"": 100,
            ""high_water"": 1400
          },
          ""configuration"": {
            ""max_memory_factor"": 0.5,
            ""max_clients"": 512,
            ""capacity"": 200
          }
        }
      },
      ""mssql"": {
        ""free"": {
          ""job_management"": {
            ""high_water"": 1400,
            ""low_water"": 100
          },
          ""configuration"": {
            ""allow_over_provisioning"": true,
            ""capacity"": 200,
            ""max_db_size"": 128,
            ""max_long_query"": 3,
            ""max_long_tx"": 0,
            ""max_clients"": 20,
            ""lifecycle"": {
              ""enable"": true,
              ""worker_count"": 1,
              ""snapshot"": {
                ""quota"": 10
              }
            }
          }
        }
      },
      ""uhurufs"": {
        ""free"": {
          ""job_management"": {
            ""high_water"": 1400,
            ""low_water"": 100
          },
          ""configuration"": {
            ""capacity"": 200,
            ""max_memory"": 16,
            ""max_swap"": 32,
            ""max_clients"": 500
          }
        }
      }
    },
    ""mysql_gateway"": {
      ""supported_versions"": [
        ""5.1""
      ],
      ""version_aliases"": {
        ""current"": ""5.1""
      },
      ""check_orphan_interval"": 7200,
      ""token"": ""Alg82gD2mXjtyq5B"",
      ""node_timeout"": 60,
      ""service_timeout"": 30
    },
    ""mysql_node"": {
      ""supported_versions"": [
        ""5.1""
      ],
      ""default_versions"": ""5.1"",
      ""production"": true,
      ""password"": ""aaa13djkas"",
      ""op_time_limit"": 50
    },
    ""redis_gateway"": {
      ""supported_versions"": [
        ""2.2""
      ],
      ""version_aliases"": {
        ""current"": ""2.2""
      },
      ""token"": ""gQ1mjpqdzhljRGju"",
      ""check_orphan_interval"": 7200,
      ""node_timeout"": 60,
      ""service_timeout"": 30
    },
    ""redis_node"": {
      ""supported_versions"": [
        ""2.2""
      ],
      ""default_versions"": ""2.2"",
      ""op_time_limit"": 50
    },
    ""mongodb_gateway"": {
      ""supported_versions"": [
        ""1.8"",
        ""2.0""
      ],
      ""version_aliases"": {
        ""current"": ""2.0"",
        ""deprecated"": ""1.8""
      },
      ""check_orphan_interval"": 7200,
      ""token"": ""ELcjgTsd7Tsfpcnz"",
      ""node_timeout"": 60,
      ""service_timeout"": 30
    },
    ""mongodb_node"": {
      ""supported_versions"": [
        ""1.8"",
        ""2.0""
      ],
      ""default_version"": ""1.8"",
      ""op_time_limit"": 50
    },
    ""rabbit_gateway"": {
      ""supported_versions"": [
        ""2.4""
      ],
      ""version_aliases"": {
        ""current"": ""2.4""
      },
      ""check_orphan_interval"": 7200,
      ""token"": ""9rxCftgnofi0GXZ6"",
      ""node_timeout"": 60,
      ""service_timeout"": 30
    },
    ""rabbit_node"": {
      ""supported_versions"": [
        ""2.4""
      ],
      ""default_versions"": ""2.4"",
      ""op_time_limit"": 50
    },
    ""postgresql_gateway"": {
      ""check_orphan_interval"": 7200,
      ""token"": ""L01xwjdGIhHH0I4L"",
      ""supported_versions"": [
        ""9.0""
      ],
      ""version_aliases"": {
        ""current"": ""9.0""
      }
    },
    ""postgresql_node"": {
      ""production"": true,
      ""supported_versions"": [
        ""9.0""
      ],
      ""default_version"": ""9.0""
    },
    ""syslog_aggregator"": {
      ""address"": ""10.134.0.153"",
      ""port"": 54321
    },
    ""report_processor"": {
      ""syslog_aggregator"": {
        ""address"": ""10.134.0.153"",
        ""port"": 54321
      }
    },
    ""stager"": {
      ""max_staging_duration"": 600,
      ""max_active_tasks"": 10,
      ""queues"": [
        ""staging""
      ]
    },
    ""uaadb"": {
      ""address"": ""10.134.0.159"",
      ""port"": 2544,
      ""roles"": [
        {
          ""tag"": ""admin"",
          ""name"": ""root"",
          ""password"": ""OPSLMp7nrq7g""
        }
      ],
      ""databases"": [
        {
          ""tag"": ""uaa"",
          ""name"": ""uaa""
        }
      ]
    },
    ""uaa"": {
      ""cc"": {
        ""token_secret"": ""K8ZzbApdyQCQ"",
        ""client_secret"": ""pGxqqbLhpZCX""
      },
      ""admin"": {
        ""client_secret"": ""x6PJgtTTfiX5""
      },
      ""login"": {
        ""client_secret"": ""LqhuTbXHeRZP""
      },
      ""clients"": {
        ""dashboard"": {
          ""id"": ""dashboard"",
          ""secret"": ""xKlVoYLgXBFU"",
          ""authorized-grant-types"": ""authorization_code,refresh_token"",
          ""scope"": ""openid,dashboard.user"",
          ""authorities"": ""uaa.resource,tokens.read,tokens.write"",
          ""access-token-validity"": 5000
        },
        ""dashborad_admin"": {
          ""id"": ""dashboard_admin"",
          ""secret"": ""somesecret"",
          ""authorized-grant-types"": ""client_credentials"",
          ""scope"": ""uaa.none"",
          ""authorities"": ""scim.read,scim.write,tokens.read,tokens.write,clients.read,clients.write,uaa.admin"",
          ""access-token-validity"": 5000
        }
      },
      ""scim"": {
        ""users"": [
          ""admin|eRihCu1V|admin@uhurucloud.net|Dash|Board|openid,dashboard.user""
        ]
      }
    }
  },
  ""template_hashes"": {
    ""win_dea"": ""1e4703bf82d15bf96245acd50e8628a6994fa05a""
  }
}";
            dynamic prop = JsonConvert.DeserializeObject(currentProp);
            string robj = Job.GetRubyObject(prop);
            Console.WriteLine(robj);

        }
    }
}
