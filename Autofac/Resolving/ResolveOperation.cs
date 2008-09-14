﻿using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Autofac.Registry;
using Autofac.Lifetime;
using Autofac.Services;

namespace Autofac.Resolving
{
    class ResolveOperation : IComponentContext
    {
        IComponentRegistry _componentRegistry;
        Stack<ComponentActivation> _activationStack = new Stack<ComponentActivation>();
        ICollection<ComponentActivation> _successfulActivations;
        ISharingLifetimeScope _mostNestedLifetimeScope;
        CircularDependencyDetector _circularDependencyDetector = new CircularDependencyDetector();
        
        public ResolveOperation(ISharingLifetimeScope mostNestedLifetimeScope, IComponentRegistry componentRegistry)
        {
            _mostNestedLifetimeScope = Enforce.ArgumentNotNull(mostNestedLifetimeScope, "mostNestedLifetimeScope");
            _componentRegistry = Enforce.ArgumentNotNull(componentRegistry, "componentRegistry");
            ResetSuccessfulActivations();
        }

        ISharingLifetimeScope CurrentActivationScope
        {
            get
            {
                if (_activationStack.Any())
                    return _activationStack.Peek().ActivationScope;
                else
                    return _mostNestedLifetimeScope;
            }
        }

        public object Resolve(IComponentRegistration registration, IEnumerable<Parameter> parameters)
        {
            Enforce.ArgumentNotNull(registration, "registration");
            Enforce.ArgumentNotNull(parameters, "parameters");

            _circularDependencyDetector.CheckForCircularDependency(registration, _activationStack);

            object instance = null;

            var activation = new ComponentActivation(registration, this, CurrentActivationScope);

            _activationStack.Push(activation);
            try
            {
                instance = activation.Execute(parameters);
                _successfulActivations.Add(activation);
            }
            finally
            {
                _activationStack.Pop();
            }

            CompleteActivations();

            return instance;
        }

        void CompleteActivations()
        {
            var completed = _successfulActivations;
            ResetSuccessfulActivations();

            foreach (var activation in completed)
                activation.Complete();
        }

        void ResetSuccessfulActivations()
        {
            _successfulActivations = new List<ComponentActivation>();
        }

        public IComponentRegistry ComponentRegistry
        {
            get { return _componentRegistry; }
        }
    }
}
