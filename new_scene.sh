
idx=$1
scene_name=$2

if [ -z $1 ]; then
	echo "ERROR: idx not given (args are: idx, scene_name)"
	return
fi

if [ -z $2 ]; then
	echo "ERROR: scene_name not given (args are: idx, scene_name)"
	return
fi

scene_template_file="$(pwd)/Assets/Scenes/_SceneTemplates/_template.unity"
scene_final_path="$(pwd)/Assets/Scenes/${idx}-${scene_name}/Scene${scene_name}.unity"
scene_folder_name="$(pwd)/Assets/Scenes/${idx}-${scene_name}"
scene_script_folder="$(pwd)/Assets/Scenes/${idx}-${scene_name}/Scripts"

echo $scene_template_file
echo $scene_final_path
echo $scene_folder_name
echo $scene_script_folder

mkdir "${scene_folder_name}"
cp "$scene_template_file" "$scene_final_path"
mkdir "$scene_script_folder"
touch "$scene_script_folder/.gitkeep"