<?php
//if (!isset($_POST["json"])){
//    die("No json exits...");
//}

$requestBody = file_get_contents("php://input");
if (!$requestBody) {
    die("No JSON received...");
}
$request = json_decode($requestBody);

$response = new STDClass();

// Validate request action
if (!isset($request->action)) {
    $response->serverMessage = "No valid server action";
    echo json_encode($response);
    exit;
}

// Handle actions based on request
switch ($request->action) {
    case "create_account":
        CreateAccount($request);
        break;
    default:
        $response->serverMessage = "No valid server action";
        echo json_encode($response);
        break;
}

function CreateAccount($request) {
    global $response;

    require_once("connect.php");

    // Check if the email already exists
    $stmt = $conn->prepare("SELECT id FROM users WHERE email = :email");
    $stmt->bindParam(":email", $request->email);
    $stmt->execute();
    
    if ($stmt->fetchColumn() > 0) {
        $response->errorMessage = "Email already exists";
        echo json_encode($response);
        return;
    }

    // Hash the password
    $hash = password_hash($request->password, PASSWORD_DEFAULT);

    // Insert the new user
    $stmt = $conn->prepare("INSERT INTO users (email, hash) VALUES (:email, :hash)");
    $stmt->bindValue(":email", $request->email);
    $stmt->bindValue(":hash", $hash);

    // Execute the insert and handle errors
    if ($stmt->execute()) {
        $response->serverMessage = "Account Creation Success";
    } else {
        $response->errorMessage = "Failed to create account";
    }

    echo json_encode($response);
}
?>