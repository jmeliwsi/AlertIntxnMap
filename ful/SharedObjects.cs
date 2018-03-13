using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
    /// <summary>
    /// Web Service Return Codes.  Needed by libaries and applications outside FusionWebService solution.
    /// Moved to here to keep it one place.  Use FUL.ReturnCode to access.
    /// </summary>
    public enum ReturnCode { 
        FAILURE,                        // call to service failed.
        EXCEPTION,                      // call to service resulted in unspecified exception
        XML_EXCEPTION,                  // call to service failed when parsing XML.  There is a format or XSD violation
        INVALID_KEY,                    // call to service failed because a key created from the parameters is not valid.
        KEY_NOT_FOUND,                  // call to service failed because a key created from the parameters was not found
        SUCCESS,                        // call to servcie succeeded.
        SERVER_OK,                      // call to service reports the server is okay
        DB_UNAVAILABLE,                 // call to service failed, the data base was not available to responding to calls to it.
        DB_INSERT_FAILED,               // call to serivce failed on insert into the data base
        DB_UPDATE_FAILED,               // call to service failed on update to the data base
        DB_UPDATE_WARNING,              // call to service succeeded with a warning status on update to the data base.
        LOGIN_FAILED,                   // call to service failed username, password and customer id do not match valid credentials
        UNSUPPORTED_VERSION,            // call to service failed due to a data or interface version mismatch
        UNPARSABLE_ROUTE,               // call to service failed due to the flight route not being parsable.
        DB_DELETE_WARNING,              // call to service succedded with a warning status on delete
        DOCUMENT_ALREADY_EXISTS,        // call to service failed because document already existed and this was not an update
        DOCUMENT_NOT_FOUND,             // call to servcie failed the specified document could not be found.
        DB_INSERT_FAILED_DUPLICATE_KEY, // call to service failed because there was a duplicate key found during insert to the data base.
        UNAUTHORIZED,                   // call to service failed because user tried to execute an unauthorized action
        DB_UPDATE_INCOMPLETE            // call to service failed when the data base update was incomplete.
    };

    public enum DeltaReturnCode
    {
        EXCEPTION,
        MESSAGE_SEND_FAIL,
        SUCCESS,
        UNAUTHORIZED,
        XML_EXCEPTION,
    }

}
