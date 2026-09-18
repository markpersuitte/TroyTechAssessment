namespace TroyTechAssessment.Helpers;
    public static class Extentions{
        extension(string)
        {
            public static bool EqualsAny(string a, string[] b,
                StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase){
                foreach (var s in b){
                    if (string.Equals(a, s, stringComparison)){
                        return true;
                    }
                }
                return false;
            }
        }
    }
