# ABSTRACT
The Helm deployment is organized to support different kinds of controllers (Deployment, StatefulSet etc.), default values, and overrides per controller.
Global env vars and local (per controller vars) are combined.
Local tags if present, override global tag.
Ports indicate there is a need of Service. Ports in the controller and in the Service are identical.
Topics names are being configured as an env vars, both at senders and receivers
# USAGE
From this directory, if need just to printout the generated yamls:
````
helm template cartoiot . -f environments/values-<env>.yaml
````

If need to apply to the currently connected k8s cluster:
````
helm upgrade --install cloudpillarmgr ./path-to-your-chart -f environments/values-<env>.yaml
````