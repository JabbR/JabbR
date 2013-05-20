using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JabbR.Services;
using Microsoft.Owin.Security.DataProtection;

namespace JabbR.Infrastructure
{
    public class JabbRDataProtection : IDataProtector
    {
        private readonly ICryptoService _cryptoService;
        public JabbRDataProtection(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        public byte[] Protect(byte[] userData)
        {
            return _cryptoService.Protect(userData);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return _cryptoService.Unprotect(protectedData);
        }
    }
}