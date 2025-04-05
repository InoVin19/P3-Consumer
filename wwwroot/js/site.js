// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/videoHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Start the connection
connection.start().then(function () {
    console.log("Connected to SignalR hub");
}).catch(function (err) {
    console.error("Error connecting to SignalR hub:", err.toString());
    return console.error(err.toString());
});

// Handle new video uploads
connection.on("VideoUploaded", function (video) {
    console.log("New video uploaded:", video);
    
    // Refresh the page to show the new video
    // In a more advanced implementation, we would dynamically add the video to the grid
    location.reload();
});

// Handle video preview on hover
document.addEventListener('DOMContentLoaded', function () {
    const videoCards = document.querySelectorAll('.video-card');
    
    videoCards.forEach(card => {
        const preview = card.querySelector('.video-preview');
        
        if (preview) {
            // Set preview to start at 0 and only play for 10 seconds
            preview.currentTime = 0;
            
            card.addEventListener('mouseenter', function () {
                preview.play();
                
                // Stop after 10 seconds
                setTimeout(() => {
                    if (!preview.paused) {
                        preview.pause();
                    }
                }, 10000);
            });
            
            card.addEventListener('mouseleave', function () {
                preview.pause();
                preview.currentTime = 0;
            });
        }
    });
    
    // Update queue status
    updateQueueStatus();
});

// Update queue status bar
function updateQueueStatus() {
    const queueCount = parseInt(document.getElementById('queueCount').textContent);
    const queueLimit = parseInt(document.getElementById('queueLimit').textContent);
    const progressFill = document.querySelector('.progress-fill');
    
    if (progressFill) {
        const percentage = (queueCount / queueLimit) * 100;
        progressFill.style.width = `${percentage}%`;
        
        // Change color based on fill level
        if (percentage > 80) {
            progressFill.style.backgroundColor = '#dc3545'; // Red
        } else if (percentage > 50) {
            progressFill.style.backgroundColor = '#ffc107'; // Yellow
        } else {
            progressFill.style.backgroundColor = '#28a745'; // Green
        }
    }
}
