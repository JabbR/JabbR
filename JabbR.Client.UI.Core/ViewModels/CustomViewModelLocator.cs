using System;
using System.Reflection;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using Newtonsoft.Json;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class CustomViewModelLocator
        : IMvxViewModelLocator
    {
        public bool TryLoad(Type viewModelType, IMvxBundle parameterValues, IMvxBundle savedState, out IMvxViewModel model)
        {
            model = null;

            // create the new ViewModel
            // here we use Activator.CreateInstance but this could also be done using an IoC container
            var newViewModel = (IMvxViewModel)Mvx.IocConstruct(viewModelType);

            // find the NavgiateTo method
            var initMethod = viewModelType.GetTypeInfo().GetDeclaredMethod("Init");
            if (initMethod == null)
            {
                MvxTrace.Trace(MvxTraceLevel.Error, "Missing Init method in ViewModel");
                return false;
            }

            var navigateToParameters = initMethod.GetParameters();
            if (navigateToParameters.Length > 1)
            {
                MvxTrace.Trace(MvxTraceLevel.Error, "Missing Init method has too many parameters {0} - expecting zero or one", navigateToParameters.Length);
                return false;
            }

            if (navigateToParameters.Length == 0)
            {
                initMethod.Invoke(newViewModel, new object[0]);
            }
            else
            {
                var navigationParameter = navigateToParameters[0];
                
                if (!typeof(NavigationParametersBase).GetTypeInfo().IsAssignableFrom(navigationParameter.ParameterType.GetTypeInfo()))
                {
                    MvxTrace.Trace(MvxTraceLevel.Error, "The parameter for NavigateTo must inherit from NavigationParametersBase");
                    return false;
                }

                if (!parameterValues.Data.ContainsKey(NavigationParametersBase.Key))
                {
                    MvxTrace.Trace(MvxTraceLevel.Error, "The Navigaton was missing the parameter for {0}", NavigationParametersBase.Key);
                    return false;
                }

                var text = parameterValues.Data[NavigationParametersBase.Key];
                var navigationParameterObject = JsonConvert.DeserializeObject(text, navigationParameter.ParameterType);

                initMethod.Invoke(newViewModel, new object[] { navigationParameterObject });
            }

            model = (IMvxViewModel)newViewModel;
            return true;
        }
    }
}
