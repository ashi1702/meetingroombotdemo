using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;

namespace MeetingRoomBot.Helpers
{
    public class JWTTokenParser
    {
        public static string GetUserEmail(string token)
        {
            if (token != null)
            {
                var jwt = new JwtSecurityToken(token);
                return jwt.Payload.Claims.Where(c => c.Type.Equals("upn", StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault()?.Value;
            }
            else
            {
                return null;
            }
        }
    }
}