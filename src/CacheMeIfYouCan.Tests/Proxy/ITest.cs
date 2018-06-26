﻿using System.Threading.Tasks;

namespace CacheMeIfYouCan.Tests.Proxy
{
    public interface ITest
    {
        Task<string> StringToString(string key);

        Task<string> IntToString(int key);

        Task<double> DoubleToDouble(double key);
    }
}