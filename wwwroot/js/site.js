// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
console.log("Site.js loaded - TIMESTAMP: " + new Date().toISOString());

// Add a global mousemove listener to verify event handling is working
document.addEventListener('mousemove', function(e) {
    // Only log once every 2 seconds to avoid flooding the console
    if (!window.lastMouseMoveLog || (new Date() - window.lastMouseMoveLog) > 2000) {
        console.log("Mouse is moving at coordinates:", e.clientX, e.clientY);
        window.lastMouseMoveLog = new Date();
    }
});

document.addEventListener('DOMContentLoaded', function () {
    console.log("DOM fully loaded - TIMESTAMP: " + new Date().toISOString());
    console.log("Document ready state:", document.readyState);
    
    // Log all video cards found on the page
    const videoCards = document.querySelectorAll('.video-card');
    console.log("Found video cards:", videoCards.length);
    videoCards.forEach((card, index) => {
        console.log(`Video card ${index}:`, card);
        const video = card.querySelector('video');
        if (video) {
            console.log(`Video element in card ${index}:`, video);
        } else {
            console.log(`No video element found in card ${index}`);
        }
    });
    
    // Set up SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/videoHub")
        .build();
    
    // Start the connection
    connection.start().catch(function (err) {
        console.error("SignalR connection error:", err.toString());
    });
    
    // Handle new video uploads
    connection.on("VideoUploaded", function (video) {
        console.log("New video uploaded:", video);
        // Refresh the page to show the new video
        // In a more advanced implementation, we would dynamically add the video to the grid
        location.reload();
    });
    
    // Handle queue update
    connection.on("QueueUpdated", function(queueCount, queueLimit) {
        document.getElementById('queueCount').textContent = queueCount;
        document.getElementById('queueLimit').textContent = queueLimit;
        
        // Update progress bar
        const percentage = queueLimit > 0 ? (queueCount * 100 / queueLimit) : 0;
        document.querySelector('.progress-bar').style.width = percentage + '%';
    });
    
    // Handle video preview on hover - direct approach
    videoCards.forEach(card => {
        console.log("Setting up hover for card:", card);
        
        card.addEventListener('mouseenter', function() {
            console.log("DIRECT mouseenter on card:", this);
            const video = this.querySelector('video');
            if (video) {
                console.log("Playing video directly:", video.src);
                video.currentTime = 0;
                video.muted = true;
                video.play().catch(e => console.error("Error playing video:", e));
                
                // Stop after 10 seconds
                setTimeout(() => {
                    if (!video.paused) {
                        console.log("10 seconds reached, pausing video");
                        video.pause();
                        video.currentTime = 0;
                    }
                }, 10000);
            }
        });
        
        card.addEventListener('mouseleave', function() {
            console.log("DIRECT mouseleave on card:", this);
            const video = this.querySelector('video');
            if (video) {
                console.log("Pausing video directly");
                video.pause();
                video.currentTime = 0;
            }
        });
    });
    
    // Also try the delegation approach as a fallback
    setupVideoPreview();
});

function setupVideoPreview() {
    console.log("Setting up video previews via delegation - TIMESTAMP: " + new Date().toISOString());
    
    // Force load all videos
    const allVideos = document.querySelectorAll('video');
    console.log("Total videos found:", allVideos.length);
    allVideos.forEach(video => {
        console.log("Loading video:", video.src);
        video.load();
    });
    
    // Use event delegation for video card hover
    document.addEventListener('mouseover', function(event) {
        // Find if we're hovering over a video card or one of its children
        const videoCard = event.target.closest('.video-card');
        if (!videoCard) {
            // Don't log this as it floods the console
            return;
        }
        
        console.log('DELEGATION: Hovering over video card:', videoCard);
        
        // Check if we're already tracking this hover
        if (videoCard.dataset.hovering === 'true') return;
        
        // Mark as hovering
        videoCard.dataset.hovering = 'true';
        
        const video = videoCard.querySelector('video');
        if (video) {
            console.log('DELEGATION: Found video element, playing:', video.src);
            // Reset to beginning
            video.currentTime = 0;
            // Ensure it's muted
            video.muted = true;
            // Play the video
            const playPromise = video.play();
            if (playPromise !== undefined) {
                playPromise.catch(e => console.error('Error playing video:', e));
            }
            
            // Set up a timer to stop after 10 seconds
            videoCard.previewTimer = setTimeout(() => {
                if (!video.paused) {
                    console.log('DELEGATION: Video reached 10 seconds, pausing');
                    video.pause();
                    video.currentTime = 0;
                }
                videoCard.dataset.hovering = 'false';
            }, 10000);
        } else {
            console.log('DELEGATION: No video element found in card');
        }
    });
    
    document.addEventListener('mouseout', function(event) {
        // Find if we're leaving a video card
        const videoCard = event.target.closest('.video-card');
        if (!videoCard) return;
        
        // Check if the related target is still within the card
        const relatedTarget = event.relatedTarget;
        if (videoCard.contains(relatedTarget)) return;
        
        console.log('DELEGATION: Mouse leaving video card');
        
        // Clear hovering state
        videoCard.dataset.hovering = 'false';
        
        // Clear the timeout
        if (videoCard.previewTimer) {
            clearTimeout(videoCard.previewTimer);
        }
        
        const video = videoCard.querySelector('video');
        if (video) {
            console.log('DELEGATION: Mouse leave on video card, pausing video');
            video.pause();
            video.currentTime = 0;
        }
    });
    
    // Handle play button click
    document.addEventListener('click', function(event) {
        // Check if we clicked a play button
        const playBtn = event.target.closest('.play-btn');
        if (!playBtn) return;
        
        event.preventDefault();
        event.stopPropagation();
        
        const videoCard = playBtn.closest('.video-card');
        const videoId = videoCard.dataset.videoId;
        const videoTitle = videoCard.querySelector('.card-title').textContent;
        
        console.log('Play button clicked for video:', videoId);
        
        // Set modal title and video source
        document.getElementById('videoPlayerModalLabel').textContent = videoTitle;
        document.getElementById('modalVideoPlayer').src = `/video/${videoId}`;
        
        // Show the modal
        const videoModal = new bootstrap.Modal(document.getElementById('videoPlayerModal'));
        videoModal.show();
        
        // Pause any preview videos
        document.querySelectorAll('video').forEach(video => {
            video.pause();
        });
    });
    
    // Pause modal video when modal is closed
    const videoPlayerModal = document.getElementById('videoPlayerModal');
    if (videoPlayerModal) {
        videoPlayerModal.addEventListener('hidden.bs.modal', function () {
            const modalVideo = document.getElementById('modalVideoPlayer');
            if (modalVideo) {
                modalVideo.pause();
                modalVideo.currentTime = 0;
            }
        });
    }
}

// Update queue status bar
function updateQueueStatus() {
    const queueCount = parseInt(document.getElementById('queueCount').textContent);
    const queueLimit = parseInt(document.getElementById('queueLimit').textContent);
    
    // Update progress bar
    const percentage = queueLimit > 0 ? (queueCount * 100 / queueLimit) : 0;
    document.querySelector('.progress-bar').style.width = percentage + '%';
    
    // Update color based on capacity
    const progressBar = document.querySelector('.progress-bar');
    if (percentage < 50) {
        progressBar.classList.remove('bg-warning', 'bg-danger');
        progressBar.classList.add('bg-success');
    } else if (percentage < 80) {
        progressBar.classList.remove('bg-success', 'bg-danger');
        progressBar.classList.add('bg-warning');
    } else {
        progressBar.classList.remove('bg-success', 'bg-warning');
        progressBar.classList.add('bg-danger');
    }
}
