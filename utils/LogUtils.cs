using System;
using Colossal.Logging;

namespace ZoningToolkitMod.Utilties {
    public static class LogUtils {
        public static ILog getLogger<T>(this T typeClass) {
            if (typeClass.GetType().IsClass) {
                return LogManager.GetLogger(typeClass.GetType().FullName);
            } else {
                throw new Exception("Logger can only be created for System.IO.Reflections.Types");
            }
        }
    }
}