# Master Thesis Leon

Uses  [*AR Foundation 4.0*](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.0/manual/index.html) 

The following library has been used for this repository:

* Unity Google Drive ([link](https://github.com/Elringus/UnityGoogleDrive))
* UniGLTF ([link](https://github.com/ousttrue/UniGLTF))

See also (Not in this repo, for external analysis of point cloud):
* Open3D ([link](http://www.open3d.org/))

My Master Thesis is about using Augmented Reality to track construction progress.

The 3D CAD Model is superimposed on the real world object by implement image tracking based on marker.
The image is the 3D CAD Model used in this work.

![alt text](https://github.com/leonrevon/MasterThesisLeon/blob/main/image/CADModel.png?raw=true)


There are two scenes in this work, point cloud scene and meshing scene (Using LiDAR).
In Point Cloud scene, the application will track the feature points of the object of interest.

![4mdn8j](https://user-images.githubusercontent.com/26881328/99151828-0c6b9f00-269e-11eb-8631-fc20f0911a4c.gif)


In the Meshing scene, using collected mesh data from LiDAR sensor, the application analyses and shows the percentage/ confidence level whether the object parts are available or not. The material of the virtual object will change to green if the system detects that the parts are there. The GUI shows number of mesh data collected, Ground truth collected, percentage of each part (Confidence level) and also parts that are undetected/ not visually presented in the scene.

![4mdpv8](https://user-images.githubusercontent.com/26881328/99152170-62414680-26a0-11eb-9764-2235d004bd4e.gif)


