using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;

namespace Uhuru.BOSH.Agent
{
    /// <summary>
    /// The unity provider
    /// </summary>
    public class UnityProvider
    {
        private static volatile UnityProvider instance;
        private static object locker = new object();
        IUnityContainer unityContainer;

        private UnityProvider()
        {
            //configure unity
            unityContainer = new UnityContainer();
            UnityConfigurationSection configurationSerction = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            configurationSerction.Configure(unityContainer);

        }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetProvider<T>()
        {
            return unityContainer.Resolve<T>();
        }

        public static UnityProvider GetInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                            instance = new UnityProvider();
                    }
                }
                return instance;
            }
        }
    }
}
