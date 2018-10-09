# Mitsubishi_robotic_arm_middleware_run
A simple tray application that encapsulates the middleware MelfaRXM for Mitsubishi robotic arms

**Notice that MelfaRXM by Mitsubishi for its industrial robotic arms is a proprietary software that one needs to get a license to use, and it is not part of this project.** 

For more detail of the middleware, one is referred to [this paper](https://ieeexplore.ieee.org/application/enterprise/entconfirmation.jsp?arnumber=6089341&icp=false).
The project only contains minimal code that wraps and initialize the middleware in a Windows tray application. Anyone who wants to use the project use extend the "middleware" class for more functionality.
