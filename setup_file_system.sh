unity_base_path="$(pwd)"
unity_assets_path="${unity_base_path}/Assets"

echo "Assets path is: ${unity_assets_path}"


cd "$unity_assets_path"

######## SCRIPTING ########
echo "Creating scripting folder ... "
mkdir Scripts
cd Scripts

mkdir _Packages
cd _Packages
touch .gitkeep
cd ..

mkdir Types
cd Types
touch .gitkeep
cd ..

mkdir Components
cd Components
touch .gitkeep
cd ..

mkdir Utils
cd Utils
touch .gitkeep
cd ..

cd ..
echo "Creating scripting folder ... OK"


######## DLL SCRIPTING ########
echo "Creating DLL assets folder ... "
mkdir DLLFiles
cd DLLFiles
touch .gitkeep
cd ..
echo "Creating DLL assets folder ... OK"


######## MRTK2 PROFILES FOLDER ########
echo "Creating MRTK2 'profiles' folder ... "
cd MRTK

mkdir profiles
cd profiles
touch .gitkeep
cd ..

cd ..
echo "Creating DLL assets folder ... OK"


######## RESOURCES FOLDER ########
echo "Creating Resources folder ... "
mkdir Resources
cd Resources

mkdir VisualAPIBaseScripts
cd VisualAPIBaseScripts

mkdir Types
cd Types
touch .gitkeep
cd ..

mkdir Components
cd Components
touch .gitkeep
cd ..

mkdir Utils
cd Utils
touch .gitkeep
cd ..

cd ..

cd ..
echo "Creating Resources folder ... OK"


######## ENTRY POINT SCENE ########
echo "Creating scene 'EntryPoint' ... "
cd Scenes
cp ./SampleScene.unity ./EntryPoint.unity
mkdir _SceneTemplates
mv ./SampleScene.unity ./_SceneTemplates/_template.unity
cd ..
echo "Creating scene 'EntryPoint' ... OK"


######## MAIN STUB SCENE ########
echo "Creating scene 'Main' ... "
cd Scenes
mkdir ./01-Main
cp ./_SceneTemplates/_template.unity ./01-Main/SceneMain.unity
mkdir ./01-Main/Scripts
touch ./01-Main/Scripts/.gitkeep
cd ..
echo "Creating scene 'Main' ... OK"


cd ..