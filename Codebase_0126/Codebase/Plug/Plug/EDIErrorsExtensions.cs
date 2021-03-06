﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public static class EDIErrorsExtensions
    {
        public static void AddFunctionalGroupDetails(this InterchangeErrors errors, int groupNumber, string idCode, string ctrlNumber, bool updateExisting)
        {
            if (errors == null)
                throw new ArgumentNullException("errors");

            FunctionalGroupErrors functionalGroupErrors = new FunctionalGroupErrors(groupNumber, idCode, ctrlNumber);

            if (errors.FunctionalGroupErrors.Count > 0 && updateExisting == true)
            {
                errors.FunctionalGroupErrors.Clear();
            }

            errors.FunctionalGroupErrors.Add(functionalGroupErrors);
        }

        public static void AddTransactionSetDetails(this InterchangeErrors errors, int tsNumber, string idCode, string ctrlNumber, bool updateExisting)
        {
            if (errors == null)
                throw new ArgumentNullException("errors");
            
            TransactionSetErrors transactionSetErrors = new TransactionSetErrors(tsNumber, idCode, ctrlNumber, new EdiSectionErrors());

            FunctionalGroupErrors functionalGroupErrors;

            if (errors.FunctionalGroupErrors.Count == 0)
            {
                // TODO: Handle this error condition properly - Need to consider scenario where ST segment appear before GS
                // this should be error since TransactionSetDetails should be added only after functionalGroup

                functionalGroupErrors = errors.CreateNewFunctionalGroupErrorInfo(-1, "---", "----");
            }
            else
                functionalGroupErrors = errors.FunctionalGroupErrors.Last();

            if (functionalGroupErrors.TransactionSetErrors.Count > 0 && updateExisting == true)
            {
                functionalGroupErrors.TransactionSetErrors.Clear();
            }

            functionalGroupErrors.TransactionSetErrors.Add(transactionSetErrors);
        }

        public static void AddGenericError(this InterchangeErrors errors, string segmentName, int errorCode, string errorDescription, int segmentNo)
        {
            errors.AddGenericError(segmentName, errorCode, errorDescription, segmentNo , - 1, -1);
        }

        public static void AddGenericError(this InterchangeErrors errors, string segmentName, int errorCode, string errorDescription, int segmentNo, 
            long startIndex, long endIndex)
        {
            if (errors == null)
                throw new ArgumentNullException("errors");

            GenericError genericError = new GenericError(errorCode, errorDescription, segmentNo, segmentName, startIndex, endIndex);

            EdiSectionErrors ediSectionErrors = GetEdiSectionErrors(errors, segmentName);

            ediSectionErrors.GenericErrorList.Add(genericError);
        }

        public static void AddSegmentError(this InterchangeErrors errors, string segmentName, int errorCode, string errorDescription,
                int segmentNumber, long startIndex, long endIndex, EdiErrorType errorType)
        {
            if (errors == null)
                throw new ArgumentNullException("errors");

            string explicitLoopId = "";
            SegmentError segmentError = new SegmentError(segmentName, segmentNumber, errorCode, errorDescription, 
                    explicitLoopId, startIndex, endIndex, errorType);

            EdiSectionErrors ediSectionErrors = GetEdiSectionErrors(errors, segmentName);

            ediSectionErrors.SegmentErrorList.Add(segmentError);
        }

        public static void AddFieldError(this InterchangeErrors errors, string segmentName, string fieldName, int errorCode, string errorDescription, int segmentNo, 
                int positionInSegment, string fieldValue, long startIndex, long endIndex, EdiErrorType errorType)
        {
            if (errors == null)
                throw new ArgumentNullException("errors");

            int positionInField = -1;
            int repetitionCount = 0;
            string refDesignator = "";

            if (errors == null)
                throw new ArgumentNullException("errors");

            FieldError fieldError = new FieldError(string.Format("{0}-{1}", segmentName, fieldName), positionInSegment, positionInField, 
                    repetitionCount, errorCode, errorDescription, segmentNo, 
                    fieldValue, refDesignator, startIndex, endIndex, errorType);

            EdiSectionErrors ediSectionErrors = GetEdiSectionErrors(errors, segmentName);

            ediSectionErrors.FieldErrorList.Add(fieldError);
        }

        private static EdiSectionErrors GetEdiSectionErrors(InterchangeErrors errors, string segmentName)
        {
            EdiSectionErrors ediSectionErrors;

            switch (segmentName.ToUpper())
            {
                case "ISA":
                case "IEA":
                    ediSectionErrors = errors.IsaIeaErrorList;
                    break;

                case "GS":
                case "GE":
                    // FunctionalGroupErrors will have at least 1 element when GS segment is parsed.
                    // If we encounter error during GS segment itself then add error in ISaIeaErrorList
                    if (errors.FunctionalGroupErrors.Count == 0)
                        ediSectionErrors = errors.IsaIeaErrorList;
                    else
                    {
                        ediSectionErrors = errors.FunctionalGroupErrors.Last().GsGeErrorList;
                    }
                    break;

                default:
                    // FunctionalGroupErrors will have at least 1 element when GS segment is parsed.
                    // Similararly TransactionSetErrors will have at least 1 element when ST segment is parsed.
                    // If we encounter error during any segment and then check the order in reverse way
                    if (errors.FunctionalGroupErrors.Count == 0)
                        ediSectionErrors = errors.IsaIeaErrorList;
                    else if (errors.FunctionalGroupErrors.Last().TransactionSetErrors.Count == 0)
                    {
                        ediSectionErrors = errors.FunctionalGroupErrors.Last().GsGeErrorList;
                    }
                    else
                    {
                        ediSectionErrors = errors.FunctionalGroupErrors.Last().TransactionSetErrors.Last().TsErrorList;
                    }
                    break;
            }

            return ediSectionErrors;
        }
    }
}
